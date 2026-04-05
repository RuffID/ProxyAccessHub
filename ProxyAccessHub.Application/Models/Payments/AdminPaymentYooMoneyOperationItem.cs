namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Операция YooMoney, найденная во время сверки заявки.
/// </summary>
public sealed record AdminPaymentYooMoneyOperationItem(
    string OperationId,
    string Status,
    string StatusName,
    decimal ActualAmountRub,
    DateTimeOffset OccurredAtUtc,
    string Label,
    string Title,
    bool IsAlreadyAttached,
    bool CanApply);
