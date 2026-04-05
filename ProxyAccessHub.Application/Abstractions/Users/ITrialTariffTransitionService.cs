namespace ProxyAccessHub.Application.Abstractions.Users;

/// <summary>
/// Обрабатывает автопереключение пользователей после завершения trial.
/// </summary>
public interface ITrialTariffTransitionService
{
    /// <summary>
    /// Завершает истёкшие trial-назначения и переключает пользователей на следующий тариф.
    /// </summary>
    /// <param name="nowUtc">Момент проверки в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Количество обработанных пользователей.</returns>
    Task<int> ProcessExpiredTrialsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);
}
