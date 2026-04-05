using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProxyAccessHub.Application.Abstractions.Users;

namespace ProxyAccessHub.Infrastructure.Users;

/// <summary>
/// Фоновая обработка истёкших trial-назначений тарифов.
/// </summary>
public class TrialTariffTransitionBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TrialTariffTransitionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CHECK_INTERVAL = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceScopeFactory.CreateScope();
                ITrialTariffTransitionService transitionService = scope.ServiceProvider.GetRequiredService<ITrialTariffTransitionService>();
                int processedUsers = await transitionService.ProcessExpiredTrialsAsync(DateTimeOffset.UtcNow, stoppingToken);

                if (processedUsers > 0)
                {
                    logger.LogInformation(
                        "Автопереключение после завершения trial обработало {ProcessedUsers} пользователей.",
                        processedUsers);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Ошибка фоновой обработки истёкших trial-назначений.");
            }

            await Task.Delay(CHECK_INTERVAL, stoppingToken);
        }
    }
}
