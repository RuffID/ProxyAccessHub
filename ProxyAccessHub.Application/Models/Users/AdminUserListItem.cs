namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Строка пользователя для таблицы минимальной админки.
/// </summary>
/// <param name="UserId">Локальный идентификатор пользователя.</param>
/// <param name="TelemtUserId">Идентификатор пользователя в telemt.</param>
/// <param name="ServerName">Название сервера пользователя.</param>
/// <param name="TariffId">Идентификатор текущего тарифа.</param>
/// <param name="TariffName">Название текущего тарифа.</param>
/// <param name="TariffRequiresRenewal">Признак тарифа с периодическим продлением.</param>
/// <param name="EffectivePeriodPriceRub">Фактическая стоимость периода в рублях с учётом индивидуальных настроек.</param>
/// <param name="BalanceRub">Текущий баланс пользователя в рублях.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="RequiresManualHandling">Признак активной ручной обработки.</param>
/// <param name="ManualHandlingStatusName">Отображаемый статус ручной обработки.</param>
/// <param name="ManualHandlingReason">Причина ручной обработки.</param>
/// <param name="CustomPeriodPriceRub">Индивидуальная фиксированная цена периода в рублях.</param>
/// <param name="DiscountPercent">Индивидуальная скидка в процентах.</param>
public sealed record AdminUserListItem(
    Guid UserId,
    string TelemtUserId,
    string ServerName,
    Guid TariffId,
    string TariffName,
    bool TariffRequiresRenewal,
    decimal EffectivePeriodPriceRub,
    decimal BalanceRub,
    DateTimeOffset? AccessPaidToUtc,
    bool RequiresManualHandling,
    string ManualHandlingStatusName,
    string? ManualHandlingReason,
    decimal? CustomPeriodPriceRub,
    decimal? DiscountPercent);
