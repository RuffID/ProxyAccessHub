namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Вариант фильтра по статусу заявки на странице платежей.
/// </summary>
public sealed record AdminPaymentRequestStatusOption(
    string Code,
    string Name);
