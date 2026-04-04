namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель тарифа для хранения в базе данных.
/// </summary>
public class TariffDefinitionEntity
{
    /// <summary>
    /// Идентификатор тарифа.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Название тарифа.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Стоимость полного периода в рублях.
    /// </summary>
    public decimal PeriodPriceRub { get; set; }

    /// <summary>
    /// Длительность периода в месяцах.
    /// </summary>
    public int PeriodMonths { get; set; }

    /// <summary>
    /// Признак безлимитного тарифа.
    /// </summary>
    public bool IsUnlimited { get; set; }

    /// <summary>
    /// Признак обязательного продления по периодам.
    /// </summary>
    public bool RequiresRenewal { get; set; }

    /// <summary>
    /// Признак активности тарифа.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Признак тарифа по умолчанию.
    /// </summary>
    public bool IsDefault { get; set; }
}
