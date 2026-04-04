namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Предложение для сценария создания нового подключения.
/// </summary>
/// <param name="TariffId">Идентификатор тарифа нового подключения.</param>
/// <param name="TariffName">Название тарифа нового подключения.</param>
/// <param name="PeriodMonths">Длительность первого оплачиваемого периода в месяцах.</param>
/// <param name="AmountRub">Стоимость первого оплачиваемого периода в рублях.</param>
/// <param name="ServerName">Название сервера, на который будет создан пользователь.</param>
public sealed record NewConnectionOffer(
    Guid TariffId,
    string TariffName,
    int PeriodMonths,
    decimal AmountRub,
    string ServerName);
