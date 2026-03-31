namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Общие настройки приложения.
/// </summary>
public sealed class ProxyAccessHubOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "ProxyAccessHub";

    /// <summary>
    /// Время жизни запроса на оплату в минутах.
    /// </summary>
    public int PaymentRequestLifetimeMinutes { get; init; }

    /// <summary>
    /// Общий лимит пользователей на один proxy-сервер.
    /// </summary>
    public int MaxUsersPerServer { get; init; }
}
