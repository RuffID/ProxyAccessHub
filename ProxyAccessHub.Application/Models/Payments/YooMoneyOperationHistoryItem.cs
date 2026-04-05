namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Операция из истории кошелька YooMoney с фактической суммой поступления по wallet API.
/// </summary>
public sealed record YooMoneyOperationHistoryItem(
    string OperationId,
    string Status,
    string Direction,
    decimal AmountRub,
    DateTimeOffset OccurredAtUtc,
    string Label,
    string Title);
