namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки пула proxy-серверов.
/// </summary>
public sealed class ProxyServerPoolOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "ProxyServerPool";

    /// <summary>
    /// Общий лимит пользователей на один сервер.
    /// </summary>
    public int DefaultMaxUsersPerServer { get; init; }
}
