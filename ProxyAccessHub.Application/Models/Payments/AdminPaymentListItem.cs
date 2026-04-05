namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Строка заявки на оплату для административной таблицы.
/// </summary>
public sealed record AdminPaymentListItem(
    Guid PaymentRequestId,
    string Label,
    Guid UserId,
    string TelemtUserId,
    Guid ServerId,
    string ServerName,
    decimal RequestedAmountRub,
    decimal AttachedAmountRub,
    int AttachedPaymentCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? LastPaymentAtUtc,
    string StatusCode,
    string StatusName,
    bool IsSuccessful);
