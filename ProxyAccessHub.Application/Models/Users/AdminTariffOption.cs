namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Тариф, доступный для выбора в админке.
/// </summary>
/// <param name="Id">Идентификатор тарифа.</param>
/// <param name="Name">Отображаемое название тарифа.</param>
/// <param name="RequiresRenewal">Признак тарифа с периодическим продлением.</param>
public sealed record AdminTariffOption(
    Guid Id,
    string Name,
    bool RequiresRenewal);
