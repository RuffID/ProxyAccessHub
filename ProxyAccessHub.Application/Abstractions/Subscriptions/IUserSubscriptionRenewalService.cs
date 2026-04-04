using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Subscriptions;

/// <summary>
/// Применяет платёж к балансу пользователя и рассчитывает новое состояние подписки.
/// </summary>
public interface IUserSubscriptionRenewalService
{
    /// <summary>
    /// Применяет входящий платёж к пользователю и его подписке.
    /// </summary>
    /// <param name="user">Текущее состояние пользователя.</param>
    /// <param name="currentSubscription">Текущее состояние подписки пользователя.</param>
    /// <param name="paymentAmountRub">Сумма входящего платежа в рублях.</param>
    /// <param name="calculatedAtUtc">Момент расчёта в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённые данные пользователя и подписки.</returns>
    Task<UserSubscriptionRenewalResult> ApplyAsync(
        ProxyUser user,
        Subscription? currentSubscription,
        decimal paymentAmountRub,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken = default);
}
