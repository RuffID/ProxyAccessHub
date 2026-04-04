namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Текущая подписка пользователя.
/// </summary>
/// <param name="Id">Локальный идентификатор подписки.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="TariffId">Идентификатор тарифа подписки.</param>
/// <param name="StartedAtUtc">Дата начала подписки в UTC.</param>
/// <param name="PaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="IsUnlimited">Признак безлимитной подписки.</param>
public record Subscription(
    Guid Id,
    Guid UserId,
    Guid TariffId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? PaidToUtc,
    bool IsUnlimited);
