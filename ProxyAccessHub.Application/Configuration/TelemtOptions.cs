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
    /// Значение заголовка Authorization для API telemt.
    /// </summary>
    public string AuthorizationHeader { get; init; } = string.Empty;

    /// <summary>
    /// Включает фоновую синхронизацию пользователей.
    /// </summary>
    public bool SyncEnabled { get; init; }

    /// <summary>
    /// Интервал фоновой синхронизации в минутах.
    /// </summary>
    public int SyncIntervalMinutes { get; init; }
}
