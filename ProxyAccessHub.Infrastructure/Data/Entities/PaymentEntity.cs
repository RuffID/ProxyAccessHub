using EFCoreLibrary.Abstractions.Entity;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель входящего платежа.
/// </summary>
public class PaymentEntity : IEntity<Guid>
{
    /// <summary>
    /// Локальный идентификатор платежа.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор заявки на оплату.
    /// </summary>
    public Guid PaymentRequestId { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Идентификатор операции у платёжного провайдера.
    /// </summary>
    public string ProviderOperationId { get; set; } = string.Empty;

    /// <summary>
    /// Сумма платежа в рублях.
    /// </summary>
    public decimal AmountRub { get; set; }

    /// <summary>
    /// Дата получения платежа в UTC.
    /// </summary>
    public DateTimeOffset ReceivedAtUtc { get; set; }

    /// <summary>
    /// Статус применения платежа.
    /// </summary>
    public PaymentStatus Status { get; set; }
}
