namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Состояние создания нового подключения по локальной заявке на оплату.
/// </summary>
/// <param name="PaymentRequestId">Идентификатор заявки на оплату.</param>
/// <param name="Label">Метка заявки у платёжного провайдера.</param>
/// <param name="AmountRub">Сумма заявки в рублях.</param>
/// <param name="StatusName">Отображаемое имя текущего статуса.</param>
/// <param name="StatusMessage">Подсказка по текущему статусу.</param>
/// <param name="IsCompleted">Признак завершённого создания подключения.</param>
/// <param name="RequiresManualHandling">Признак перевода кейса в ручную обработку.</param>
/// <param name="ExpiresAtUtc">Дата истечения заявки в UTC.</param>
/// <param name="TelemtUserId">Идентификатор созданного пользователя в telemt.</param>
/// <param name="ProxyLink">Созданная proxy-ссылка пользователя.</param>
/// <param name="TariffName">Название тарифа созданного пользователя.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="ManualHandlingReason">Причина ручной обработки, если она требуется.</param>
public sealed record NewConnectionPaymentStatusResult(
    Guid PaymentRequestId,
    string Label,
    decimal AmountRub,
    string StatusName,
    string StatusMessage,
    bool IsCompleted,
    bool RequiresManualHandling,
    DateTimeOffset ExpiresAtUtc,
    string? TelemtUserId,
    string? ProxyLink,
    string TariffName,
    DateTimeOffset? AccessPaidToUtc,
    string? ManualHandlingReason);
