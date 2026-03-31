using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Telemt;

namespace ProxyAccessHub.Infrastructure.Telemt;

/// <summary>
/// Фоновая периодическая синхронизация пользователей из telemt.
/// </summary>
public class TelemtUsersSyncBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<TelemtOptions> telemtOptions,
    ILogger<TelemtUsersSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!telemtOptions.Value.SyncEnabled)
        {
            return;
        }

        if (telemtOptions.Value.SyncIntervalMinutes <= 0)
        {
            throw new InvalidOperationException("Интервал фоновой синхронизации telemt должен быть больше нуля.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ITelemtUsersSyncService syncService = scope.ServiceProvider.GetRequiredService<ITelemtUsersSyncService>();
            ITelemtSyncStateStore syncStateStore = scope.ServiceProvider.GetRequiredService<ITelemtSyncStateStore>();

            try
            {
                TelemtUsersSyncResult result = await syncService.SyncAsync(stoppingToken);
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
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                syncStateStore.SetFailure(ex.Message, DateTimeOffset.UtcNow);
                logger.LogError(ex, "Ошибка фоновой синхронизации пользователей из telemt.");
            }

            await Task.Delay(TimeSpan.FromMinutes(telemtOptions.Value.SyncIntervalMinutes), stoppingToken);
        }
    }
}
