namespace ProxyAccessHub.Application.Configuration;

/// <summary>
/// Описание тарифа в конфигурации.
/// </summary>
public sealed class TariffDefinitionOptions
{
    /// <summary>
    /// Код тарифа.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Отображаемое название тарифа.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Стоимость полного периода в рублях.
    /// </summary>
    public decimal PeriodPriceRub { get; init; }

    /// <summary>
    /// Длительность периода в месяцах.
    /// </summary>
    public int PeriodMonths { get; init; } = 1;

    /// <summary>
    /// Признак безлимитного тарифа.
    /// </summary>
    public bool IsUnlimited { get; init; }

    /// <summary>
    /// Признак обязательного продления по периодам.
    /// </summary>
    public bool RequiresRenewal { get; init; } = true;
}
