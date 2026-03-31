using ProxyAccessHub.Application.Models.Tariffs;

namespace ProxyAccessHub.Application.Abstractions.Tariffs;

/// <summary>
/// Выполняет базовый расчёт продления по тарифу.
/// </summary>
public interface ITariffRenewalCalculator
{
    /// <summary>
    /// Рассчитывает количество полных периодов и остаток баланса после продления.
    /// </summary>
    /// <param name="request">Исходные данные расчёта.</param>
    /// <returns>Результат расчёта продления.</returns>
    TariffRenewalCalculationResult Calculate(TariffRenewalCalculationRequest request);
}
