using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Users;

/// <summary>
/// Обслуживает пользовательский сценарий создания нового подключения.
/// </summary>
public interface IUserConnectionCreationService
{
    /// <summary>
    /// Возвращает предложение для создания нового подключения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные предложения для экрана создания подключения.</returns>
    Task<NewConnectionOffer> GetOfferAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт локального ожидающего пользователя и заявку на оплату нового подключения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные формы оплаты YooMoney.</returns>
    Task<YooMoneyPaymentFormModel> CreatePaymentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает текущее состояние заявки на оплату нового подключения.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор заявки на оплату.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Статус создания нового подключения.</returns>
    Task<NewConnectionPaymentStatusResult> GetPaymentStatusAsync(Guid paymentRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Завершает создание нового подключения после успешной оплаты.
    /// </summary>
    /// <param name="pendingUser">Локальный ожидающий пользователь.</param>
    /// <param name="paidAtUtc">Момент подтверждённой оплаты в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные созданного подключения.</returns>
    Task<NewConnectionProvisioningResult> ProvisionPaidConnectionAsync(
        ProxyUser pendingUser,
        DateTimeOffset paidAtUtc,
        CancellationToken cancellationToken = default);
}
