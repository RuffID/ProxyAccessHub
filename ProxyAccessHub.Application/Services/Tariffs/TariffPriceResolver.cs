using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Tariffs;

/// <summary>
/// Вычисляет итоговую цену периода по тарифу.
/// </summary>
public sealed class TariffPriceResolver : ITariffPriceResolver
{
    /// <inheritdoc />
    public decimal ResolvePeriodPrice(TariffDefinition tariff, TariffUserPriceOverride? priceOverride)
    {
        if (!tariff.RequiresRenewal)
        {
            return 0m;
        }

        if (priceOverride is null)
        {
            return tariff.PeriodPriceRub;
        }

        if (priceOverride.CustomPeriodPriceRub is not null && priceOverride.DiscountPercent is not null)
        {
            throw new InvalidOperationException($"Для тарифа '{tariff.Id}' нельзя одновременно задать фиксированную цену и скидку.");
        }

        if (priceOverride.CustomPeriodPriceRub is decimal customPeriodPriceRub)
        {
            if (customPeriodPriceRub <= 0m)
            {
                throw new InvalidOperationException($"Для тарифа '{tariff.Id}' индивидуальная цена периода должна быть больше нуля.");
            }

            return decimal.Round(customPeriodPriceRub, 2, MidpointRounding.AwayFromZero);
        }

        if (priceOverride.DiscountPercent is decimal discountPercent)
        {
            if (discountPercent <= 0m || discountPercent >= 100m)
            {
                throw new InvalidOperationException($"Для тарифа '{tariff.Id}' скидка должна быть больше нуля и меньше ста процентов.");
            }

            decimal discountedPrice = tariff.PeriodPriceRub * (100m - discountPercent) / 100m;

            if (discountedPrice <= 0m)
            {
                throw new InvalidOperationException($"Для тарифа '{tariff.Id}' итоговая цена периода должна быть больше нуля.");
            }

            return decimal.Round(discountedPrice, 2, MidpointRounding.AwayFromZero);
        }

        return tariff.PeriodPriceRub;
    }
}
