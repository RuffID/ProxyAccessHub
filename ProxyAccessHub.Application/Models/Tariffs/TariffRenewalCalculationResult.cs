namespace ProxyAccessHub.Application.Models.Tariffs;

/// <summary>
/// Результат расчёта продления тарифа.
/// </summary>
/// <param name="TariffId">Идентификатор тарифа, для которого выполнен расчёт.</param>
/// <param name="EffectivePeriodPriceRub">Эффективная цена полного периода в рублях.</param>
/// <param name="RenewalApplied">Признак успешного списания одного периода и продления доступа.</param>
/// <param name="ChargedAmountRub">Сумма, списываемая за продление.</param>
/// <param name="RemainingBalanceRub">Остаток баланса после списания одного периода.</param>
/// <param name="RenewalAppliedFromUtc">Момент, от которого начинается продление в UTC.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа после расчёта в UTC.</param>
/// <param name="RequiresRenewal">Признак обязательного продления по периодам.</param>
public sealed record TariffRenewalCalculationResult(
    Guid TariffId,
    decimal EffectivePeriodPriceRub,
    bool RenewalApplied,
    decimal ChargedAmountRub,
    decimal RemainingBalanceRub,
    DateTimeOffset? RenewalAppliedFromUtc,
    DateTimeOffset? AccessPaidToUtc,
    bool RequiresRenewal);
