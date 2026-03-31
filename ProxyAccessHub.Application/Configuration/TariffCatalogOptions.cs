namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Настройки каталога тарифов.
/// </summary>
public sealed class TariffCatalogOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "Tariffs";

    /// <summary>
    /// Код тарифа по умолчанию.
    /// </summary>
    public string DefaultTariffCode { get; init; } = string.Empty;

    /// <summary>
    /// Список доступных тарифов.
    /// </summary>
    public IReadOnlyList<TariffDefinitionOptions> Items { get; init; } = Array.Empty<TariffDefinitionOptions>();
}
