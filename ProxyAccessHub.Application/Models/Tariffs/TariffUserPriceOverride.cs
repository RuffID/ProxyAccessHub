namespace ProxyAccessHub.Application.Models.Tariffs;

/// <summary>
/// Индивидуальные условия цены пользователя для конкретного тарифа.
/// </summary>
/// <param name="CustomPeriodPriceRub">Индивидуальная фиксированная цена периода в рублях.</param>
/// <param name="DiscountPercent">Индивидуальная скидка в процентах.</param>
public sealed record TariffUserPriceOverride(
    decimal? CustomPeriodPriceRub,
    decimal? DiscountPercent);
