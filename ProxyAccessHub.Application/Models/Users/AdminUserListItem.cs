namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Строка пользователя для таблицы минимальной админки.
/// </summary>
/// <param name="UserId">Локальный идентификатор пользователя.</param>
/// <param name="TelemtUserId">Идентификатор пользователя в telemt.</param>
/// <param name="ProxyLink">Ссылка подключения пользователя.</param>
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
/// <param name="HasTrialHistory">Признак наличия trial в истории пользователя.</param>
/// <param name="HasActiveTrial">Признак активного trial.</param>
/// <param name="TariffAssignedAtUtc">Дата начала текущего назначения тарифа в UTC.</param>
/// <param name="TrialEndsAtUtc">Дата завершения активного trial в UTC.</param>
/// <param name="NextTariffName">Название тарифа после завершения trial.</param>
/// <param name="TariffAssignmentComment">Комментарий текущего назначения тарифа.</param>
/// <param name="TariffAssignedBy">Источник или инициатор текущего назначения.</param>
public sealed record AdminUserListItem(
    Guid UserId,
    string TelemtUserId,
    string ProxyLink,
    string ServerName,
    Guid TariffId,
    string TariffName,
    bool TariffRequiresRenewal,
    decimal EffectivePeriodPriceRub,
    decimal BalanceRub,
    DateTimeOffset? AccessPaidToUtc,
    bool IsTelemtAccessActive,
    bool RequiresManualHandling,
    string ManualHandlingStatusName,
    string? ManualHandlingReason,
    decimal? CustomPeriodPriceRub,
    decimal? DiscountPercent,
    bool HasTrialHistory,
    bool HasActiveTrial,
    DateTimeOffset? TariffAssignedAtUtc,
    DateTimeOffset? TrialEndsAtUtc,
    string? NextTariffName,
    string? TariffAssignmentComment,
    string? TariffAssignedBy);
