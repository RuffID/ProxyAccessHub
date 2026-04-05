using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Administration;

/// <summary>
/// Управляет административным просмотром платежей и сверкой с YooMoney.
/// </summary>
public interface IAdminPaymentManagementService
{
    /// <summary>
    /// Возвращает данные страницы платежей.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные страницы платежей.</returns>
    Task<AdminPaymentsPageData> GetPageDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Выполняет сверку заявки с операциями YooMoney.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор заявки на оплату.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Детали сверки для модального окна.</returns>
    Task<AdminPaymentCheckDetails> CheckAsync(Guid paymentRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает из YooMoney отсутствующие операции заявки и автоматически применяет их к пользователю.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор заявки на оплату.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<AdminPaymentCheckDetails> ApplyMissingOperationsAsync(Guid paymentRequestId, CancellationToken cancellationToken = default);
}
