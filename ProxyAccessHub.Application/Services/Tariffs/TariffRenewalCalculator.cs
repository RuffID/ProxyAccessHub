using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Tariffs;

namespace ProxyAccessHub.Application.Services.Tariffs;

/// <summary>
/// Выполняет базовый расчёт продления с учётом только полных периодов.
/// </summary>
public sealed class TariffRenewalCalculator(ITariffPriceResolver tariffPriceResolver) : ITariffRenewalCalculator
{
    /// <inheritdoc />
    public TariffRenewalCalculationResult Calculate(TariffRenewalCalculationRequest request)
    {
        if (request.BalanceRub < 0m)
        {
            throw new InvalidOperationException("Баланс пользователя не может быть отрицательным.");
        }

        if (!request.Tariff.RequiresRenewal)
        {
            return new TariffRenewalCalculationResult(
                request.Tariff.Id,
                0m,
                0,
                0m,
                request.BalanceRub,
                null,
                request.AccessPaidToUtc,
                false);
        }

        decimal effectivePeriodPriceRub = tariffPriceResolver.ResolvePeriodPrice(request.Tariff, request.PriceOverride);

        if (effectivePeriodPriceRub <= 0m)
        {
            throw new InvalidOperationException($"Для тарифа '{request.Tariff.Id}' цена периода должна быть больше нуля.");
        }

        int purchasedPeriods = decimal.ToInt32(decimal.Floor(request.BalanceRub / effectivePeriodPriceRub));
        decimal chargedAmountRub = purchasedPeriods * effectivePeriodPriceRub;
        decimal remainingBalanceRub = request.BalanceRub - chargedAmountRub;

        if (remainingBalanceRub < 0m)
        {
            throw new InvalidOperationException("Остаток баланса после расчёта не может быть отрицательным.");
        }

        if (purchasedPeriods == 0)
        {
            return new TariffRenewalCalculationResult(
                request.Tariff.Id,
                effectivePeriodPriceRub,
                0,
                0m,
                request.BalanceRub,
                null,
                request.AccessPaidToUtc,
                true);
        }

        DateTimeOffset renewalAppliedFromUtc = request.AccessPaidToUtc is { } accessPaidToUtc && accessPaidToUtc > request.CalculatedAtUtc
            ? accessPaidToUtc
            : request.CalculatedAtUtc;
        DateTimeOffset newAccessPaidToUtc = TariffPeriodHelper.ApplyPeriods(
            renewalAppliedFromUtc,
            request.Tariff.PeriodMonths,
            purchasedPeriods);

        return new TariffRenewalCalculationResult(
            request.Tariff.Id,
            effectivePeriodPriceRub,
            purchasedPeriods,
            chargedAmountRub,
            remainingBalanceRub,
            renewalAppliedFromUtc,
            newAccessPaidToUtc,
            true);
    }
}
