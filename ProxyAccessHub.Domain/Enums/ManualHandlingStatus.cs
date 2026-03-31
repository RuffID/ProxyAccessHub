namespace ProxyAccessHub.Domain.Enums;

/// <summary>
/// Статус необходимости ручной обработки пользователя или платежа.
/// </summary>
public enum ManualHandlingStatus
{
    /// <summary>
    /// Ручная обработка не требуется.
    /// </summary>
    NotRequired = 0,

    /// <summary>
    /// Случай требует ручной обработки.
    /// </summary>
    Required = 1,

    /// <summary>
    /// Ручная обработка завершена.
    /// </summary>
    Completed = 2
}
