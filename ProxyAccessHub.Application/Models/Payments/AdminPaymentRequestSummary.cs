using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Краткая сводка по заявке на оплату для ручной сверки.
/// </summary>
public sealed record AdminPaymentRequestSummary(
    Guid PaymentRequestId,
    string Label,
    Guid UserId,
    string TelemtUserId,
    Guid ServerId,
    string ServerName,
    decimal RequestedAmountRub,
    decimal AttachedAmountRub,
    PaymentRequestStatus PaymentRequestStatus,
    string PaymentRequestStatusName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
