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
    /// Идентификатор OAuth-приложения ЮMoney.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Секрет OAuth-приложения ЮMoney.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Адрес возврата OAuth-авторизации ЮMoney.
    /// </summary>
    public string RedirectUri { get; init; } = string.Empty;

    /// <summary>
    /// OAuth-токен wallet API для ручной сверки операций.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Дата окончания действия OAuth-токена wallet API в UTC.
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresAtUtc { get; init; }
}
