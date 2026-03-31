namespace ProxyAccessHub.Application.Models.Tariffs;

/// <summary>
/// Исходные данные для расчёта продления тарифа.
/// </summary>
/// <param name="Tariff">Тариф пользователя.</param>
/// <param name="BalanceRub">Текущий баланс пользователя в рублях.</param>
/// <param name="CalculatedAtUtc">Момент расчёта в UTC.</param>
/// <param name="AccessPaidToUtc">Текущая дата оплаченного доступа в UTC.</param>
/// <param name="PriceOverride">Индивидуальные условия цены пользователя.</param>
public sealed record TariffRenewalCalculationRequest(
    TariffPlan Tariff,
    decimal BalanceRub,
    DateTimeOffset CalculatedAtUtc,
    DateTimeOffset? AccessPaidToUtc,
    TariffUserPriceOverride? PriceOverride);
