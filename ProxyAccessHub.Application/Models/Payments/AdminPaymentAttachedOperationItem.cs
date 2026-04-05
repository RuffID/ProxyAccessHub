namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Платёж, уже привязанный к локальной заявке.
/// </summary>
public sealed record AdminPaymentAttachedOperationItem(
    Guid PaymentId,
    string ProviderOperationId,
    decimal AmountRub,
    decimal? ActualAmountRub,
    DateTimeOffset ReceivedAtUtc);
