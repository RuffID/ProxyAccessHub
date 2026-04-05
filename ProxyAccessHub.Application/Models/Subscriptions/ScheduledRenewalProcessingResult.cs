namespace ProxyAccessHub.Application.Models.Subscriptions;

/// <summary>
/// Сводный результат планового списания за очередной период доступа.
/// </summary>
/// <param name="ProcessedUsers">Количество обработанных пользователей с наступившим сроком оплаты.</param>
/// <param name="RenewedUsers">Количество пользователей, которым успешно продлён доступ.</param>
/// <param name="ActivatedUsers">Количество пользователей, автоматически активированных после успешного списания.</param>
/// <param name="DeactivatedUsers">Количество пользователей, деактивированных из-за нехватки средств.</param>
public sealed record ScheduledRenewalProcessingResult(
    int ProcessedUsers,
    int RenewedUsers,
    int ActivatedUsers,
    int DeactivatedUsers);
