namespace ProxyAccessHub.Application.Models.Administration;

/// <summary>
/// Строка тарифа для административной таблицы.
/// </summary>
public sealed record AdminTariffListItem(
    Guid Id,
    string Name,
    decimal PeriodPriceRub,
    int PeriodMonths,
    bool IsActive,
    bool IsDefault);
