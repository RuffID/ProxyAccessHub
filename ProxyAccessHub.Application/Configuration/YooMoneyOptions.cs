namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки интеграции с ЮMoney.
/// </summary>
public sealed class YooMoneyOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "YooMoney";

    /// <summary>
    /// Идентификатор кнопки сбора средств.
    /// </summary>
    public string BillNumber { get; init; } = string.Empty;

    /// <summary>
    /// Номер кошелька получателя.
    /// </summary>
    public string Receiver { get; init; } = string.Empty;

    /// <summary>
    /// Секрет для проверки уведомлений.
    /// </summary>
    public string NotificationSecret { get; init; } = string.Empty;

    /// <summary>
    /// Адрес возврата после успешной оплаты.
    /// </summary>
    public string SuccessUrl { get; init; } = string.Empty;

    /// <summary>
    /// Адрес обработчика уведомлений.
    /// </summary>
    public string NotificationUrl { get; init; } = string.Empty;
}
