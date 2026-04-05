using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий истории назначений тарифов пользователей на базе SQL Server.
/// </summary>
public class UserTariffAssignmentRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<UserTariffAssignmentEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<UserTariffAssignmentEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<UserTariffAssignmentEntity, ProxyAccessHubDbContext> queryRepository,
    ICreateItemRepository<UserTariffAssignmentEntity, ProxyAccessHubDbContext> createRepository) : IUserTariffAssignmentRepository
{
    /// <inheritdoc />
    public async Task<UserTariffAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserTariffAssignmentEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);
        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<UserTariffAssignment?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        UserTariffAssignmentEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            assignment => assignment.UserId == userId && assignment.EndedAtUtc == null,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserTariffAssignment>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        List<UserTariffAssignmentEntity> entities = await queryRepository.Query(asNoTracking: true)
            .Where(assignment => assignment.EndedAtUtc == null)
            .OrderBy(assignment => assignment.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserTariffAssignment>> GetHistoryByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<UserTariffAssignmentEntity> entities = await queryRepository.Query(asNoTracking: true)
            .Where(assignment => assignment.UserId == userId)
            .OrderByDescending(assignment => assignment.CreatedAtUtc)
            .ThenByDescending(assignment => assignment.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserTariffAssignment>> GetExpiredTrialAssignmentsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        List<UserTariffAssignmentEntity> activeTrialEntities = await queryRepository.Query(asNoTracking: true)
            .Where(assignment => assignment.IsTrial && assignment.EndedAtUtc == null)
            .OrderBy(assignment => assignment.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return activeTrialEntities
            .Select(Map)
            .Where(assignment => GetTrialEndsAtUtc(assignment) <= nowUtc)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetUserIdsWithTrialHistoryAsync(CancellationToken cancellationToken = default)
    {
        HashSet<Guid> userIds = await queryRepository.Query(asNoTracking: true)
            .Where(assignment => assignment.IsTrial)
            .Select(assignment => assignment.UserId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        return userIds;
    }

    /// <inheritdoc />
    public Task AddAsync(UserTariffAssignment assignment, CancellationToken cancellationToken = default)
    {
        ValidateAssignment(assignment);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(assignment));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(UserTariffAssignment assignment, CancellationToken cancellationToken = default)
    {
        ValidateAssignment(assignment);
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Set<UserTariffAssignmentEntity>().Update(Map(assignment));
        return Task.CompletedTask;
    }

    private static UserTariffAssignmentEntity Map(UserTariffAssignment assignment)
    {
        return new UserTariffAssignmentEntity
        {
            Id = assignment.Id,
            UserId = assignment.UserId,
            TariffId = assignment.TariffId,
            StartedAtUtc = assignment.StartedAtUtc,
            EndedAtUtc = assignment.EndedAtUtc,
            IsTrial = assignment.IsTrial,
            TrialDurationDays = assignment.TrialDurationDays,
            NextTariffId = assignment.NextTariffId,
            CreatedAtUtc = assignment.CreatedAtUtc,
            Comment = assignment.Comment,
            AssignedBy = assignment.AssignedBy
        };
    }

    private static UserTariffAssignment Map(UserTariffAssignmentEntity entity)
    {
        return new UserTariffAssignment(
            entity.Id,
            entity.UserId,
            entity.TariffId,
            entity.StartedAtUtc,
            entity.EndedAtUtc,
            entity.IsTrial,
            entity.TrialDurationDays,
            entity.NextTariffId,
            entity.CreatedAtUtc,
            entity.Comment,
            entity.AssignedBy);
    }

    private static DateTimeOffset GetTrialEndsAtUtc(UserTariffAssignment assignment)
    {
        if (!assignment.IsTrial || !assignment.TrialDurationDays.HasValue)
        {
            throw new InvalidOperationException($"Назначение '{assignment.Id}' не содержит корректной длительности trial.");
        }

        return assignment.StartedAtUtc.AddDays(assignment.TrialDurationDays.Value);
    }

    private static void ValidateAssignment(UserTariffAssignment assignment)
    {
        if (assignment.UserId == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор пользователя назначения тарифа не задан.");
        }

        if (assignment.TariffId == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор тарифа назначения не задан.");
        }

        if (assignment.IsTrial)
        {
            if (!assignment.TrialDurationDays.HasValue || assignment.TrialDurationDays.Value <= 0)
            {
                throw new InvalidOperationException("Для trial-назначения должна быть указана положительная длительность в днях.");
            }

            if (!assignment.NextTariffId.HasValue || assignment.NextTariffId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("Для trial-назначения должен быть указан следующий тариф.");
            }
        }
        else
        {
            if (assignment.TrialDurationDays.HasValue)
            {
                throw new InvalidOperationException("Для обычного назначения нельзя задавать длительность trial.");
            }

            if (assignment.NextTariffId.HasValue)
            {
                throw new InvalidOperationException("Для обычного назначения нельзя задавать следующий тариф trial.");
            }
        }

        if (assignment.EndedAtUtc.HasValue && assignment.EndedAtUtc.Value < assignment.StartedAtUtc)
        {
            throw new InvalidOperationException("Дата завершения назначения не может быть раньше даты начала.");
        }
    }
}
