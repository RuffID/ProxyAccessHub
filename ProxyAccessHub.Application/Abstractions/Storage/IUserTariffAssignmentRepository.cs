using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий истории назначений тарифов пользователям.
/// </summary>
public interface IUserTariffAssignmentRepository
{
    /// <summary>
    /// Возвращает назначение тарифа по идентификатору.
    /// </summary>
    Task<UserTariffAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает активное назначение тарифа пользователя.
    /// </summary>
    Task<UserTariffAssignment?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает активные назначения тарифов пользователей.
    /// </summary>
    Task<IReadOnlyList<UserTariffAssignment>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает историю назначений тарифа пользователя.
    /// </summary>
    Task<IReadOnlyList<UserTariffAssignment>> GetHistoryByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает истёкшие активные trial-назначения.
    /// </summary>
    Task<IReadOnlyList<UserTariffAssignment>> GetExpiredTrialAssignmentsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает идентификаторы пользователей, у которых уже был trial.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetUserIdsWithTrialHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет назначение тарифа.
    /// </summary>
    Task AddAsync(UserTariffAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет назначение тарифа.
    /// </summary>
    Task UpdateAsync(UserTariffAssignment assignment, CancellationToken cancellationToken = default);
}
