using ProxyAccessHub.Application.Models.Users;

namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Данные административной страницы платежей.
/// </summary>
public sealed record AdminPaymentsPageData(
    IReadOnlyList<AdminPaymentListItem> Payments,
    IReadOnlyList<AdminServerOption> Servers,
    IReadOnlyList<AdminPaymentRequestStatusOption> Statuses);
