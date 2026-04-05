using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Infrastructure.Telemt;

/// <summary>
/// Фоновая периодическая синхронизация пользователей из telemt.
/// </summary>
public class TelemtUsersSyncBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TelemtUsersSyncBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan DEFAULT_IDLE_DELAY = TimeSpan.FromMinutes(1);
    private readonly Dictionary<Guid, DateTimeOffset> nextSyncAtByServerId = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<ProxyServer> enabledServers = await LoadEnabledServersAsync(stoppingToken);
            RemoveObsoleteScheduleEntries(enabledServers);

            if (enabledServers.Count == 0)
            {
                await Task.Delay(DEFAULT_IDLE_DELAY, stoppingToken);
                continue;
            }

            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            ProxyServer[] dueServers = enabledServers
                .Where(server => IsDue(server, nowUtc))
                .ToArray();

            if (dueServers.Length == 0)
            {
                TimeSpan delay = GetNextDelay(enabledServers, nowUtc);
                await Task.Delay(delay, stoppingToken);
                continue;
            }

            foreach (ProxyServer server in dueServers)
            {
                try
                {
                    using IServiceScope scope = serviceScopeFactory.CreateScope();
                    ITelemtUsersSyncService syncService = scope.ServiceProvider.GetRequiredService<ITelemtUsersSyncService>();
                    ITelemtSyncStateStore syncStateStore = scope.ServiceProvider.GetRequiredService<ITelemtSyncStateStore>();
                    TelemtUsersSyncResult result = await syncService.SyncAsync(server.Id, stoppingToken);

                    nextSyncAtByServerId[server.Id] = DateTimeOffset.UtcNow.AddMinutes(server.SyncIntervalMinutes);
                    syncStateStore.SetSuccess(result);
                    logger.LogInformation(
                        "Фоновая синхронизация telemt завершена. Сервер: {ServerCode}. Ревизия: {Revision}. Обработано: {ProcessedUsers}. Создано: {CreatedUsers}. Обновлено: {UpdatedUsers}. Расхождений: {MarkedForManualHandlingUsers}.",
                        result.ServerCode,
                        result.Revision,
                        result.ProcessedUsers,
                        result.CreatedUsers,
                        result.UpdatedUsers,
                        result.MarkedForManualHandlingUsers);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    nextSyncAtByServerId[server.Id] = DateTimeOffset.UtcNow.AddMinutes(server.SyncIntervalMinutes);

                    using IServiceScope scope = serviceScopeFactory.CreateScope();
                    ITelemtSyncStateStore syncStateStore = scope.ServiceProvider.GetRequiredService<ITelemtSyncStateStore>();
                    syncStateStore.SetFailure(ex.Message, DateTimeOffset.UtcNow);
                    logger.LogError(
                        ex,
                        "Фоновая синхронизация telemt завершилась ошибкой для сервера '{ServerName}'. Следующая попытка будет через {SyncIntervalMinutes} мин.",
                        server.Name,
                        server.SyncIntervalMinutes);
                }
            }
        }
    }

    private async Task<IReadOnlyList<ProxyServer>> LoadEnabledServersAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IProxyAccessHubUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IProxyAccessHubUnitOfWork>();
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);

        return servers
            .Where(server => server.IsActive && server.SyncEnabled)
            .ToArray();
    }

    private void RemoveObsoleteScheduleEntries(IReadOnlyList<ProxyServer> enabledServers)
    {
        HashSet<Guid> activeServerIds = enabledServers.Select(server => server.Id).ToHashSet();
        Guid[] obsoleteServerIds = nextSyncAtByServerId.Keys
            .Where(serverId => !activeServerIds.Contains(serverId))
            .ToArray();

        foreach (Guid obsoleteServerId in obsoleteServerIds)
        {
            nextSyncAtByServerId.Remove(obsoleteServerId);
        }
    }

    private bool IsDue(ProxyServer server, DateTimeOffset nowUtc)
    {
        if (!nextSyncAtByServerId.TryGetValue(server.Id, out DateTimeOffset nextSyncAtUtc))
        {
            return true;
        }

        return nextSyncAtUtc <= nowUtc;
    }

    private TimeSpan GetNextDelay(IReadOnlyList<ProxyServer> enabledServers, DateTimeOffset nowUtc)
    {
        DateTimeOffset nearestSyncAtUtc = enabledServers
            .Min(server => nextSyncAtByServerId.GetValueOrDefault(server.Id, nowUtc));
        TimeSpan delay = nearestSyncAtUtc - nowUtc;

        if (delay <= TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(1);
        }

        return delay < DEFAULT_IDLE_DELAY ? delay : DEFAULT_IDLE_DELAY;
    }
}
