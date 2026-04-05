namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// История назначений тарифа пользователю.
/// </summary>
/// <param name="Id">Идентификатор назначения тарифа.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="TariffId">Идентификатор назначенного тарифа.</param>
/// <param name="StartedAtUtc">Дата начала действия назначения в UTC.</param>
/// <param name="EndedAtUtc">Дата завершения назначения в UTC.</param>
/// <param name="IsTrial">Признак триального назначения.</param>
/// <param name="TrialDurationDays">Длительность trial в днях.</param>
/// <param name="NextTariffId">Идентификатор тарифа для автопереключения после trial.</param>
/// <param name="CreatedAtUtc">Дата создания записи в UTC.</param>
/// <param name="Comment">Комментарий или причина назначения.</param>
/// <param name="AssignedBy">Источник или инициатор назначения.</param>
public sealed record UserTariffAssignment(
    Guid Id,
    Guid UserId,
    Guid TariffId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndedAtUtc,
    bool IsTrial,
    int? TrialDurationDays,
    Guid? NextTariffId,
    DateTimeOffset CreatedAtUtc,
    string? Comment,
    string? AssignedBy);
