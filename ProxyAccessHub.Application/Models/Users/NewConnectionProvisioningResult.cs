using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Результат создания нового подключения после подтверждённой оплаты.
/// </summary>
/// <param name="UpdatedUser">Обновлённый локальный пользователь.</param>
/// <param name="CreatedSubscription">Созданная локальная подписка пользователя.</param>
public sealed record NewConnectionProvisioningResult(
    ProxyUser UpdatedUser,
    Subscription CreatedSubscription);
