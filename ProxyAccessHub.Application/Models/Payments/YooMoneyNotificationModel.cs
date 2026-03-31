namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Данные HTTP-уведомления YooMoney.
/// </summary>
/// <param name="NotificationType">Тип уведомления.</param>
/// <param name="OperationId">Идентификатор операции YooMoney.</param>
/// <param name="Amount">Сумма зачисления в кошелёк.</param>
/// <param name="WithdrawAmount">Сумма списания у отправителя.</param>
/// <param name="Currency">Код валюты операции.</param>
/// <param name="DateTimeRaw">Дата и время операции в исходном формате YooMoney.</param>
/// <param name="Sender">Идентификатор отправителя.</param>
/// <param name="CodePro">Признак защищённого платежа.</param>
/// <param name="Label">Метка локальной заявки.</param>
/// <param name="Sha1Hash">Подпись уведомления.</param>
/// <param name="Unaccepted">Признак неподтверждённого зачисления.</param>
public sealed record YooMoneyNotificationModel(
    string NotificationType,
    string OperationId,
    decimal Amount,
    decimal WithdrawAmount,
    string Currency,
    string DateTimeRaw,
    string Sender,
    string CodePro,
    string Label,
    string Sha1Hash,
    string Unaccepted);
