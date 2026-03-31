using EFCoreLibrary.Abstractions.Entity;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель заявки на оплату.
/// </summary>
public class PaymentRequestEntity : IEntity<Guid>
{
    /// <summary>
    /// Локальный идентификатор заявки.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Внешний идентификатор заявки для платёжного провайдера.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Сумма заявки в рублях.
    /// </summary>
    public decimal AmountRub { get; set; }

    /// <summary>
    /// Дата создания заявки в UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>
    /// Дата истечения заявки в UTC.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Статус заявки.
    /// </summary>
    public PaymentRequestStatus Status { get; set; }
}
