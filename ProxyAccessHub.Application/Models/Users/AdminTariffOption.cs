namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Тариф, доступный для выбора в админке.
/// </summary>
/// <param name="Id">Идентификатор тарифа.</param>
/// <param name="Name">Отображаемое название тарифа.</param>
/// <param name="RequiresRenewal">Признак тарифа с периодическим продлением.</param>
/// <param name="PeriodPriceRub">Базовая стоимость полного периода в рублях.</param>
/// <param name="PeriodMonths">Длительность полного периода в месяцах.</param>
public sealed record AdminTariffOption(
    Guid Id,
    string Name,
    bool RequiresRenewal,
    decimal PeriodPriceRub,
    int PeriodMonths);
