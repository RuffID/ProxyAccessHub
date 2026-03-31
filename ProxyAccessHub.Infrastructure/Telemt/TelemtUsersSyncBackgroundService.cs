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
    private readonly TimeSpan syncInterval = TimeSpan.FromMinutes(telemtOptions.Value.SyncIntervalMinutes);

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
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                ITelemtUsersSyncService syncService = scope.ServiceProvider.GetRequiredService<ITelemtUsersSyncService>();
                ITelemtSyncStateStore syncStateStore = scope.ServiceProvider.GetRequiredService<ITelemtSyncStateStore>();
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
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                ITelemtSyncStateStore syncStateStore = scope.ServiceProvider.GetRequiredService<ITelemtSyncStateStore>();

                syncStateStore.SetFailure(ex.Message, DateTimeOffset.UtcNow);
                logger.LogError(
                    ex,
                    "Фоновая синхронизация telemt завершилась ошибкой. Следующая попытка будет через {SyncIntervalMinutes} мин.",
                    telemtOptions.Value.SyncIntervalMinutes);
            }

            await Task.Delay(syncInterval, stoppingToken);
        }
    }
}
