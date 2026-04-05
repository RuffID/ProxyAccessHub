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
    /// Лимит TCP-подключений для автоматически создаваемого пользователя.
    /// </summary>
    public int DefaultTelemtMaxTcpConnections { get; init; }

    /// <summary>
    /// Лимит уникальных IP для автоматически создаваемого пользователя.
    /// </summary>
    public int DefaultTelemtMaxUniqueIps { get; init; }
}
