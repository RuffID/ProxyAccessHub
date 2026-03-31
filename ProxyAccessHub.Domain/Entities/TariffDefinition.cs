namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Тариф, доступный для назначения пользователям.
/// </summary>
/// <param name="Code">Код тарифа.</param>
/// <param name="Name">Название тарифа.</param>
/// <param name="PeriodPriceRub">Стоимость полного периода в рублях.</param>
/// <param name="PeriodMonths">Длительность периода в месяцах.</param>
/// <param name="IsUnlimited">Признак безлимитного тарифа.</param>
/// <param name="RequiresRenewal">Признак обязательного продления по периодам.</param>
public record TariffDefinition(
    string Code,
    string Name,
    decimal PeriodPriceRub,
    int PeriodMonths,
    bool IsUnlimited,
    bool RequiresRenewal);
