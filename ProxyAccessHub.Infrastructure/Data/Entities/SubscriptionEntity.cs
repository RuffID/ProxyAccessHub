using EFCoreLibrary.Abstractions.Entity;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель подписки пользователя.
/// </summary>
public class SubscriptionEntity : IEntity<Guid>
{
    /// <summary>
    /// Локальный идентификатор подписки.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Код тарифа подписки.
    /// </summary>
    public string TariffCode { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала подписки в UTC.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// Дата оплаченного доступа в UTC.
    /// </summary>
    public DateTimeOffset? PaidToUtc { get; set; }

    /// <summary>
    /// Признак безлимитной подписки.
    /// </summary>
    public bool IsUnlimited { get; set; }
}
