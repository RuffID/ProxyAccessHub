namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Тариф, доступный для выбора в админке.
/// </summary>
/// <param name="Code">Код тарифа.</param>
/// <param name="Name">Отображаемое название тарифа.</param>
/// <param name="RequiresRenewal">Признак тарифа с периодическим продлением.</param>
public sealed record AdminTariffOption(
    string Code,
    string Name,
    bool RequiresRenewal);
