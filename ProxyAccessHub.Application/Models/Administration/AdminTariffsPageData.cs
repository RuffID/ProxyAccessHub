namespace ProxyAccessHub.Application.Models.Administration;

/// <summary>
/// Данные страницы тарифов для административного интерфейса.
/// </summary>
public sealed record AdminTariffsPageData(
    IReadOnlyList<AdminTariffListItem> Tariffs);
