using ProxyAccessHub.Application.Models.Subscriptions;

namespace ProxyAccessHub.Application.Abstractions.Subscriptions;

/// <summary>
/// Выполняет плановое списание за очередной период доступа пользователей.
/// </summary>
public interface IUserScheduledRenewalService
{
    /// <summary>
    /// Обрабатывает пользователей, у которых наступил срок очередного списания.
    /// </summary>
    /// <param name="nowUtc">Момент обработки в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сводный результат планового списания.</returns>
    Task<ScheduledRenewalProcessingResult> ProcessDueRenewalsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}
