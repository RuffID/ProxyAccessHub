namespace ProxyAccessHub.Domain.Enums;

/// <summary>
/// Статус заявки на оплату.
/// </summary>
public enum PaymentRequestStatus
{
    /// <summary>
    /// Заявка создана и ожидает оплаты.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Заявка успешно оплачена.
    /// </summary>
    Paid = 1,

    /// <summary>
    /// Срок действия заявки истёк.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Заявка отменена.
    /// </summary>
    Cancelled = 3
}
