using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий входящих платежей на базе SQL Server.
/// </summary>
public class PaymentRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<PaymentEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<PaymentEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<PaymentEntity, ProxyAccessHubDbContext> queryRepository,
    ICreateItemRepository<PaymentEntity, ProxyAccessHubDbContext> createRepository) : IPaymentRepository
{
    /// <inheritdoc />
    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        PaymentEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<PaymentEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderByDescending(payment => payment.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<Payment?> GetByProviderOperationIdAsync(string providerOperationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerOperationId))
        {
            throw new InvalidOperationException("Идентификатор операции провайдера не задан.");
        }

        PaymentEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            payment => payment.ProviderOperationId == providerOperationId.Trim(),
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payment>> GetByPaymentRequestIdAsync(Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        List<PaymentEntity> entities = await queryRepository.Query(asNoTracking: true)
            .Where(payment => payment.PaymentRequestId == paymentRequestId)
            .OrderByDescending(payment => payment.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<PaymentEntity> entities = await queryRepository.Query(asNoTracking: true)
            .Where(payment => payment.UserId == userId)
            .OrderByDescending(payment => payment.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ValidatePayment(payment);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(payment));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ValidatePayment(payment);
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Set<PaymentEntity>().Update(Map(payment));
        return Task.CompletedTask;
    }

    private static PaymentEntity Map(Payment payment)
    {
        return new PaymentEntity
        {
            Id = payment.Id,
            PaymentRequestId = payment.PaymentRequestId,
            UserId = payment.UserId,
            ProviderOperationId = payment.ProviderOperationId,
            AmountRub = payment.AmountRub,
            ActualAmountRub = payment.ActualAmountRub,
            ReceivedAtUtc = payment.ReceivedAtUtc,
            Status = payment.Status
        };
    }

    private static Payment Map(PaymentEntity entity)
    {
        return new Payment(
            entity.Id,
            entity.PaymentRequestId,
            entity.UserId,
            entity.ProviderOperationId,
            entity.AmountRub,
            entity.ActualAmountRub,
            entity.ReceivedAtUtc,
            entity.Status);
    }

    private static void ValidatePayment(Payment payment)
    {
        if (string.IsNullOrWhiteSpace(payment.ProviderOperationId))
        {
            throw new InvalidOperationException("Идентификатор операции провайдера не задан.");
        }

        if (payment.AmountRub <= 0m)
        {
            throw new InvalidOperationException("Сумма платежа должна быть больше нуля.");
        }

        if (payment.ActualAmountRub is <= 0m)
        {
            throw new InvalidOperationException("Фактическая сумма платежа должна быть больше нуля.");
        }
    }
}
