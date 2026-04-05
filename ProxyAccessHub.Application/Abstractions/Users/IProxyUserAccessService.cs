using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Users;

/// <summary>
/// Управляет активацией и деактивацией доступа пользователя в локальной системе и в telemt.
/// </summary>
public interface IProxyUserAccessService
{
    /// <summary>
    /// Активирует доступ пользователя до указанного срока.
    /// </summary>
    /// <param name="user">Текущее состояние пользователя.</param>
    /// <param name="accessPaidToUtc">Срок действия доступа после активации в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённое состояние пользователя.</returns>
    Task<ProxyUser> ActivateAsync(
        ProxyUser user,
        DateTimeOffset accessPaidToUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Деактивирует доступ пользователя.
    /// </summary>
    /// <param name="user">Текущее состояние пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённое состояние пользователя.</returns>
    Task<ProxyUser> DeactivateAsync(
        ProxyUser user,
        CancellationToken cancellationToken = default);
}
