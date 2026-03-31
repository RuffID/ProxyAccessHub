namespace ProxyAccessHub.Domain.Enums;

/// <summary>
/// Статус входящего платежа.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Платёж принят и ждёт дальнейшей обработки.
    /// </summary>
    Received = 0,

    /// <summary>
    /// Платёж применён к бизнес-логике системы.
    /// </summary>
    Applied = 1,

    /// <summary>
    /// Платёж требует ручной обработки.
    /// </summary>
    RequiresManualHandling = 2
}
