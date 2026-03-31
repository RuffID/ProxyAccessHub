using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Payments;

/// <summary>
/// Создаёт заявку на оплату продления и подготавливает форму ЮMoney.
/// </summary>
public interface IUserPaymentRequestService
{
    /// <summary>
    /// Создаёт заявку на оплату продления для пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные формы оплаты ЮMoney.</returns>
    Task<YooMoneyPaymentFormModel> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
}
