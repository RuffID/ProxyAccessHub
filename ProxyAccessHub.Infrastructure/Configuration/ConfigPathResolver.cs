namespace ProxyAccessHub.Infrastructure.Configuration;

/// <summary>
/// Определяет путь к файлу конфигурации приложения в зависимости от окружения запуска.
/// </summary>
public static class ConfigPathResolver
{
    private const string CONFIG_RELATIVE_PATH = "Config";
    private const string CONFIG_FILE_NAME = "config.json";

    /// <summary>
    /// Возвращает актуальный путь к конфигурации приложения.
    /// </summary>
    /// <param name="environmentName">Имя окружения ASP.NET Core.</param>
    /// <param name="contentRootPath">Корневой путь содержимого приложения.</param>
    /// <returns>Абсолютный путь к конфигурации.</returns>
    public static string Resolve(string? environmentName, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath))
        {
            throw new InvalidOperationException("Не задан content root path приложения.");
        }

        string basePath = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            ? contentRootPath
            : AppContext.BaseDirectory;

        return Path.Combine(basePath, CONFIG_RELATIVE_PATH, CONFIG_FILE_NAME);
    }
}
