namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки доступа обычных пользователей.
/// </summary>
public sealed class UserAccessOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "UserAccess";

    /// <summary>
    /// Общий пароль для обычных пользователей.
    /// </summary>
    public string SharedPassword { get; init; } = string.Empty;
}
