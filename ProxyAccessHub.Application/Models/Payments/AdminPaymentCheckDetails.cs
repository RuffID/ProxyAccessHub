namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Детали сверки заявки на оплату с YooMoney.
/// </summary>
public sealed record AdminPaymentCheckDetails(
    IReadOnlyList<AdminPaymentAttachedOperationItem> AttachedPayments,
    int AddedPaymentCount,
    decimal AddedAppliedAmountRub,
    decimal AddedActualAmountRub,
    int UpdatedActualAmountCount,
    bool HasLoadedYooMoneyOperations);
