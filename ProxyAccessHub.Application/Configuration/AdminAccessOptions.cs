namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки доступа администратора.
/// </summary>
public sealed class AdminAccessOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "AdminAccess";

    /// <summary>
    /// Пароль администратора.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
