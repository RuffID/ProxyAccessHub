using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Payments;

/// <summary>
/// Обрабатывает входящие HTTP-уведомления YooMoney.
/// </summary>
public interface IYooMoneyNotificationService
{
    /// <summary>
    /// Проверяет и применяет входящее уведомление YooMoney.
    /// </summary>
    /// <param name="notification">Уведомление YooMoney.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task ProcessAsync(YooMoneyNotificationModel notification, CancellationToken cancellationToken = default);
}
