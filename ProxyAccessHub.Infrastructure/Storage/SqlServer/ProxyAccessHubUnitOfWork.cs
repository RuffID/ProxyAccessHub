using EFCoreLibrary.Abstractions.Database;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Infrastructure.Data;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Реализация UnitOfWork поверх SQL Server.
/// </summary>
public class ProxyAccessHubUnitOfWork(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IProxyUserRepository users,
    IProxyServerRepository servers,
    ITariffDefinitionRepository tariffs,
    IPaymentRequestRepository paymentRequests,
    IPaymentRepository payments,
    ISubscriptionRepository subscriptions) : IProxyAccessHubUnitOfWork
{
    /// <inheritdoc />
    public IProxyUserRepository Users { get; } = users;

    /// <inheritdoc />
    public IProxyServerRepository Servers { get; } = servers;

    /// <inheritdoc />
    public ITariffDefinitionRepository Tariffs { get; } = tariffs;

    /// <inheritdoc />
    public IPaymentRequestRepository PaymentRequests { get; } = paymentRequests;

    /// <inheritdoc />
    public IPaymentRepository Payments { get; } = payments;

    /// <inheritdoc />
    public ISubscriptionRepository Subscriptions { get; } = subscriptions;

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChanges(cancellationToken);
    }
}
