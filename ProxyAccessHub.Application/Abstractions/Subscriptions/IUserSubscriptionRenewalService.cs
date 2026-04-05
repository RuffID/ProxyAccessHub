using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Subscriptions;

/// <summary>
/// Выполняет одно продление подписки по наступившему сроку оплаты.
/// </summary>
public interface IUserSubscriptionRenewalService
{
    /// <summary>
    /// Выполняет списание стоимости одного периода и рассчитывает новое состояние подписки.
    /// </summary>
    /// <param name="user">Текущее состояние пользователя.</param>
    /// <param name="currentSubscription">Текущее состояние подписки пользователя.</param>
    /// <param name="calculatedAtUtc">Момент расчёта в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённые данные пользователя и подписки.</returns>
    Task<UserSubscriptionRenewalResult> TryRenewAsync(
        ProxyUser user,
        Subscription? currentSubscription,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken = default);
}
