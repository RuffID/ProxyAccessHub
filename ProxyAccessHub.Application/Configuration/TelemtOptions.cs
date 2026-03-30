namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки подключения к серверу telemt.
/// </summary>
public sealed class TelemtOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "Telemt";

    /// <summary>
    /// Базовый адрес API telemt.
    /// </summary>
    public string ApiBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Токен доступа к API telemt.
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;
}
