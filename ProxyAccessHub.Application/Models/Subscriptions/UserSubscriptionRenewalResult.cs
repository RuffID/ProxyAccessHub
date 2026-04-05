using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Models.Subscriptions;

/// <summary>
/// Результат попытки продления подписки пользователя на один период.
/// </summary>
/// <param name="UpdatedUser">Обновлённое состояние пользователя.</param>
/// <param name="UpdatedSubscription">Обновлённое состояние подписки или <see langword="null" />, если списание не было выполнено.</param>
/// <param name="Calculation">Результат расчёта продления тарифа.</param>
public sealed record UserSubscriptionRenewalResult(
    ProxyUser UpdatedUser,
    Subscription? UpdatedSubscription,
    TariffRenewalCalculationResult Calculation);
