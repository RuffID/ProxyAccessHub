using EFCoreLibrary.Abstractions.Entity;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель истории назначений тарифа пользователю.
/// </summary>
public class UserTariffAssignmentEntity : IEntity<Guid>
{
    /// <summary>
    /// Идентификатор назначения тарифа.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор назначенного тарифа.
    /// </summary>
    public Guid TariffId { get; set; }

    /// <summary>
    /// Дата начала действия назначения в UTC.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>
    /// Дата завершения назначения в UTC.
    /// </summary>
    public DateTimeOffset? EndedAtUtc { get; set; }

    /// <summary>
    /// Признак триального назначения.
    /// </summary>
    public bool IsTrial { get; set; }

    /// <summary>
    /// Длительность trial в днях.
    /// </summary>
    public int? TrialDurationDays { get; set; }

    /// <summary>
    /// Идентификатор следующего тарифа после завершения trial.
    /// </summary>
    public Guid? NextTariffId { get; set; }

    /// <summary>
    /// Дата создания записи в UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Комментарий или причина назначения.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Источник или инициатор назначения.
    /// </summary>
    public string? AssignedBy { get; set; }
}
