using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Infrastructure.Users;

/// <summary>
/// Фоновая служба ежедневного списания за очередной период доступа пользователей.
/// </summary>
public class ScheduledRenewalBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ScheduledRenewalBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DateTimeOffset nowLocal = DateTimeOffset.Now;
            IReadOnlyList<ProxyServer> activeServers = await LoadActiveServersAsync(stoppingToken);
            TimeSpan delay = GetDelayUntilNextRun(nowLocal, activeServers);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                IUserScheduledRenewalService scheduledRenewalService = scope.ServiceProvider.GetRequiredService<IUserScheduledRenewalService>();
                IProxyAccessHubUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IProxyAccessHubUnitOfWork>();
                DateTimeOffset executionLocalTime = DateTimeOffset.Now;
                ScheduledRenewalProcessingResult result = await scheduledRenewalService.ProcessDueRenewalsAsync(DateTimeOffset.UtcNow, stoppingToken);
                await MarkServersAsProcessedAsync(unitOfWork, executionLocalTime, stoppingToken);

                logger.LogInformation(
                    "Ежедневное списание выполнено. Обработано: {ProcessedUsers}. Продлено: {RenewedUsers}. Активировано: {ActivatedUsers}. Деактивировано: {DeactivatedUsers}.",
                    result.ProcessedUsers,
                    result.RenewedUsers,
                    result.ActivatedUsers,
                    result.DeactivatedUsers);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Ошибка ежедневного списания за очередной период доступа.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task<IReadOnlyList<ProxyServer>> LoadActiveServersAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IProxyAccessHubUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IProxyAccessHubUnitOfWork>();
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);

        return servers
            .Where(server => server.IsActive)
            .ToArray();
    }

    private async Task MarkServersAsProcessedAsync(
        IProxyAccessHubUnitOfWork unitOfWork,
        DateTimeOffset processedAtLocal,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        ProxyServer[] activeServers = servers
            .Where(server => server.IsActive)
            .ToArray();

        foreach (ProxyServer server in activeServers)
        {
            ProxyServer updatedServer = server with
            {
                LastDailyRenewalProcessedDateUtc = processedAtLocal.UtcDateTime.Date
            };
            await unitOfWork.Servers.UpdateAsync(updatedServer, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan GetDelayUntilNextRun(DateTimeOffset nowLocal, IReadOnlyList<ProxyServer> activeServers)
    {
        if (activeServers.Count == 0)
        {
            return TimeSpan.FromMinutes(1);
        }

        DateOnly currentLocalDate = DateOnly.FromDateTime(nowLocal.Date);
        bool isProcessedToday = activeServers.All(server => IsProcessedForLocalDate(server, currentLocalDate, nowLocal.Offset));
        if (!isProcessedToday)
        {
            return TimeSpan.Zero;
        }

        DateTimeOffset nextRunLocal = new(
            nowLocal.Year,
            nowLocal.Month,
            nowLocal.Day,
            0,
            0,
            0,
            nowLocal.Offset);

        if (nextRunLocal <= nowLocal)
        {
            nextRunLocal = nextRunLocal.AddDays(1);
        }

        return nextRunLocal - nowLocal;
    }

    private static bool IsProcessedForLocalDate(ProxyServer server, DateOnly currentLocalDate, TimeSpan localOffset)
    {
        if (!server.LastDailyRenewalProcessedDateUtc.HasValue)
        {
            return false;
        }

        DateTimeOffset processedUtcDate = server.LastDailyRenewalProcessedDateUtc.Value;
        DateOnly processedLocalDate = DateOnly.FromDateTime(processedUtcDate.ToOffset(localOffset).Date);
        return processedLocalDate == currentLocalDate;
    }
}
