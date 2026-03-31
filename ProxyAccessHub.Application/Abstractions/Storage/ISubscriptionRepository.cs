using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий подписок пользователей.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Возвращает подписку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор подписки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная подписка или <see langword="null" />.</returns>
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает подписку пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная подписка или <see langword="null" />.</returns>
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет подписку.
    /// </summary>
    /// <param name="subscription">Подписка для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет подписку.
    /// </summary>
    /// <param name="subscription">Актуальное состояние подписки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
