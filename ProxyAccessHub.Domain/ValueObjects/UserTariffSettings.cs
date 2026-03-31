namespace ProxyAccessHub.Domain.ValueObjects;

/// <summary>
/// Индивидуальные настройки тарифа пользователя.
/// </summary>
/// <param name="CustomPeriodPriceRub">Индивидуальная фиксированная цена периода в рублях.</param>
/// <param name="DiscountPercent">Индивидуальная скидка в процентах.</param>
public record UserTariffSettings(decimal? CustomPeriodPriceRub, decimal? DiscountPercent);
