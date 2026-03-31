using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Models.Subscriptions;

/// <summary>
/// Результат применения платежа к пользователю и его подписке.
/// </summary>
/// <param name="UpdatedUser">Обновлённое состояние пользователя.</param>
/// <param name="UpdatedSubscription">Обновлённое состояние подписки или <see langword="null" />, если подписка ещё не должна создаваться.</param>
/// <param name="Calculation">Результат расчёта продления тарифа.</param>
public sealed record UserSubscriptionRenewalResult(
    ProxyUser UpdatedUser,
    Subscription? UpdatedSubscription,
    TariffRenewalCalculationResult Calculation);
