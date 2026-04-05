using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.ValueObjects;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий пользователей на базе SQL Server.
/// </summary>
public class ProxyUserRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<ProxyUserEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<ProxyUserEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<ProxyUserEntity, ProxyAccessHubDbContext> queryRepository,
    ICreateItemRepository<ProxyUserEntity, ProxyAccessHubDbContext> createRepository) : IProxyUserRepository
{
    /// <inheritdoc />
    public async Task<ProxyUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProxyUserEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<ProxyUser?> GetByTelemtUserIdAsync(string telemtUserId, CancellationToken cancellationToken = default)
    {
        string normalizedTelemtUserId = RequireValue(telemtUserId, nameof(telemtUserId));
        ProxyUserEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            user => user.TelemtUserId == normalizedTelemtUserId,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<ProxyUser?> GetByProxyLinkLookupKeyAsync(string proxyLinkLookupKey, CancellationToken cancellationToken = default)
    {
        string normalizedLookupKey = RequireValue(proxyLinkLookupKey, nameof(proxyLinkLookupKey));
        ProxyUserEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            user => user.ProxyLinkLookupKey == normalizedLookupKey,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProxyUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ProxyUserEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderBy(user => user.TelemtUserId)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public Task AddAsync(ProxyUser user, CancellationToken cancellationToken = default)
    {
        ValidateUser(user);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(user));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(ProxyUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateUser(user);
        dbContext.Set<ProxyUserEntity>().Update(Map(user));
        return Task.CompletedTask;
    }

    private static ProxyUserEntity Map(ProxyUser user)
    {
        return new ProxyUserEntity
        {
            Id = user.Id,
            TelemtUserId = user.TelemtUserId,
            ProxyLink = user.ProxyLink,
            ProxyLinkLookupKey = user.ProxyLinkLookupKey,
            ServerId = user.ServerId,
            TariffId = user.TariffId,
            CustomPeriodPriceRub = user.TariffSettings?.CustomPeriodPriceRub,
            DiscountPercent = user.TariffSettings?.DiscountPercent,
            BalanceRub = user.BalanceRub,
            AccessPaidToUtc = user.AccessPaidToUtc,
            IsTelemtAccessActive = user.IsTelemtAccessActive,
            ManualHandlingStatus = user.ManualHandlingStatus,
            ManualHandlingReason = user.ManualHandlingReason,
            UserAdTag = user.UserAdTag,
            TelemtRevision = user.TelemtRevision,
            LastSyncedAtUtc = user.LastSyncedAtUtc
        };
    }

    private static ProxyUser Map(ProxyUserEntity entity)
    {
        UserTariffSettings? tariffSettings = entity.CustomPeriodPriceRub is null && entity.DiscountPercent is null
            ? null
            : new UserTariffSettings(entity.CustomPeriodPriceRub, entity.DiscountPercent);

        return new ProxyUser(
            entity.Id,
            entity.TelemtUserId,
            entity.ProxyLink,
            entity.ProxyLinkLookupKey,
            entity.ServerId,
            entity.TariffId,
            tariffSettings,
            entity.BalanceRub,
            entity.AccessPaidToUtc,
            entity.IsTelemtAccessActive,
            entity.ManualHandlingStatus,
            entity.ManualHandlingReason,
            entity.UserAdTag,
            entity.TelemtRevision,
            entity.LastSyncedAtUtc);
    }

    private static string RequireValue(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Не задано значение '{paramName}'.");
        }

        return value.Trim();
    }

    private static void ValidateUser(ProxyUser user)
    {
        RequireValue(user.TelemtUserId, nameof(user.TelemtUserId));
        RequireValue(user.ProxyLink, nameof(user.ProxyLink));
        RequireValue(user.ProxyLinkLookupKey, nameof(user.ProxyLinkLookupKey));

        if (user.TariffId == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор тарифа пользователя не задан.");
        }

        if (user.BalanceRub < 0m)
        {
            throw new InvalidOperationException("Баланс пользователя не может быть отрицательным.");
        }
    }
}
