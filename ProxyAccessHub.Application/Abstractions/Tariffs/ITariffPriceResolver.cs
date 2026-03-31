using ProxyAccessHub.Application.Models.Tariffs;

namespace ProxyAccessHub.Application.Abstractions.Tariffs;

/// <summary>
/// Рассчитывает эффективную цену периода с учётом индивидуальных условий пользователя.
/// </summary>
public interface ITariffPriceResolver
{
    /// <summary>
    /// Возвращает итоговую цену периода.
    /// </summary>
    /// <param name="tariff">Тариф для расчёта.</param>
    /// <param name="priceOverride">Индивидуальные условия пользователя.</param>
    /// <returns>Эффективная цена периода.</returns>
    decimal ResolvePeriodPrice(TariffPlan tariff, TariffUserPriceOverride? priceOverride);
}
