namespace ProxyAccessHub.Application.Models.Tariffs;

/// <summary>
/// Нормализованная модель тарифа для расчётов приложения.
/// </summary>
/// <param name="Code">Код тарифа.</param>
/// <param name="Name">Отображаемое название тарифа.</param>
/// <param name="PeriodPriceRub">Стоимость полного периода в рублях.</param>
/// <param name="PeriodMonths">Длительность одного периода в месяцах.</param>
/// <param name="IsUnlimited">Признак безлимитного тарифа.</param>
/// <param name="RequiresRenewal">Признак обязательного продления по периодам.</param>
public sealed record TariffPlan(
    string Code,
    string Name,
    decimal PeriodPriceRub,
    int PeriodMonths,
    bool IsUnlimited,
    bool RequiresRenewal);
