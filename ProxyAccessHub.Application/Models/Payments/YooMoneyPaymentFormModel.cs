namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Данные HTML-формы оплаты ЮMoney для локальной заявки.
/// </summary>
/// <param name="PaymentRequestId">Локальный идентификатор заявки на оплату.</param>
/// <param name="FormActionUrl">Адрес отправки формы ЮMoney.</param>
/// <param name="Receiver">Номер кошелька получателя.</param>
/// <param name="Label">Внешняя метка локальной заявки.</param>
/// <param name="SumRub">Сумма перевода в рублях.</param>
/// <param name="SuccessUrl">Адрес возврата после успешной оплаты.</param>
/// <param name="ExpiresAtUtc">Дата истечения заявки в UTC.</param>
public sealed record YooMoneyPaymentFormModel(
    Guid PaymentRequestId,
    string FormActionUrl,
    string Receiver,
    string Label,
    decimal SumRub,
    string SuccessUrl,
    DateTimeOffset ExpiresAtUtc);
