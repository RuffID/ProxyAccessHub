using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий подписок пользователей на базе SQL Server.
/// </summary>
public class SubscriptionRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<SubscriptionEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<SubscriptionEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    ICreateItemRepository<SubscriptionEntity, ProxyAccessHubDbContext> createRepository) : ISubscriptionRepository
{
    /// <inheritdoc />
    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        SubscriptionEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        SubscriptionEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            subscription => subscription.UserId == userId,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        ValidateSubscription(subscription);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(subscription));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateSubscription(subscription);
        dbContext.Set<SubscriptionEntity>().Update(Map(subscription));
        return Task.CompletedTask;
    }

    private static SubscriptionEntity Map(Subscription subscription)
    {
        return new SubscriptionEntity
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            TariffId = subscription.TariffId,
            StartedAtUtc = subscription.StartedAtUtc,
            PaidToUtc = subscription.PaidToUtc,
            IsUnlimited = subscription.IsUnlimited
        };
    }

    private static Subscription Map(SubscriptionEntity entity)
    {
        return new Subscription(
            entity.Id,
            entity.UserId,
            entity.TariffId,
            entity.StartedAtUtc,
            entity.PaidToUtc,
            entity.IsUnlimited);
    }

    private static void ValidateSubscription(Subscription subscription)
    {
        if (subscription.TariffId == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор тарифа подписки не задан.");
        }

        if (!subscription.IsUnlimited && subscription.PaidToUtc is null)
        {
            throw new InvalidOperationException("Для небезлимитной подписки должна быть указана дата оплаченного доступа.");
        }
    }
}
