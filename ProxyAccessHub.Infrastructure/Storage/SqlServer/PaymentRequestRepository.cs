using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий заявок на оплату на базе SQL Server.
/// </summary>
public class PaymentRequestRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<PaymentRequestEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<PaymentRequestEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    ICreateItemRepository<PaymentRequestEntity, ProxyAccessHubDbContext> createRepository) : IPaymentRequestRepository
{
    /// <inheritdoc />
    public async Task<PaymentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        PaymentRequestEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<PaymentRequest?> GetByLabelAsync(string label, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new InvalidOperationException("Label заявки на оплату не задан.");
        }

        PaymentRequestEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            request => request.Label == label.Trim(),
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public Task AddAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default)
    {
        ValidatePaymentRequest(paymentRequest);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(paymentRequest));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePaymentRequest(paymentRequest);
        dbContext.Set<PaymentRequestEntity>().Update(Map(paymentRequest));
        return Task.CompletedTask;
    }

    private static PaymentRequestEntity Map(PaymentRequest paymentRequest)
    {
        return new PaymentRequestEntity
        {
            Id = paymentRequest.Id,
            UserId = paymentRequest.UserId,
            Label = paymentRequest.Label,
            AmountRub = paymentRequest.AmountRub,
            CreatedAtUtc = paymentRequest.CreatedAtUtc,
            ExpiresAtUtc = paymentRequest.ExpiresAtUtc,
            Status = paymentRequest.Status
        };
    }

    private static PaymentRequest Map(PaymentRequestEntity entity)
    {
        return new PaymentRequest(
            entity.Id,
            entity.UserId,
            entity.Label,
            entity.AmountRub,
            entity.CreatedAtUtc,
            entity.ExpiresAtUtc,
            entity.Status);
    }

    private static void ValidatePaymentRequest(PaymentRequest paymentRequest)
    {
        if (string.IsNullOrWhiteSpace(paymentRequest.Label))
        {
            throw new InvalidOperationException("Label заявки на оплату не задан.");
        }

        if (paymentRequest.AmountRub <= 0m)
        {
            throw new InvalidOperationException("Сумма заявки на оплату должна быть больше нуля.");
        }

        if (paymentRequest.ExpiresAtUtc <= paymentRequest.CreatedAtUtc)
        {
            throw new InvalidOperationException("Срок действия заявки должен быть позже даты создания.");
        }
    }
}
