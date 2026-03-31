using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий пользователей для сценариев продления и оплаты.
/// </summary>
public interface IProxyUserRepository
{
    /// <summary>
    /// Возвращает пользователя по локальному идентификатору.
    /// </summary>
    /// <param name="id">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный пользователь или <see langword="null" />.</returns>
    Task<ProxyUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает пользователя по идентификатору telemt.
    /// </summary>
    /// <param name="telemtUserId">Идентификатор пользователя в telemt.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный пользователь или <see langword="null" />.</returns>
    Task<ProxyUser?> GetByTelemtUserIdAsync(string telemtUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает пользователя по нормализованному ключу proxy-ссылки.
    /// </summary>
    /// <param name="proxyLinkLookupKey">Нормализованный ключ proxy-ссылки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный пользователь или <see langword="null" />.</returns>
    Task<ProxyUser?> GetByProxyLinkLookupKeyAsync(string proxyLinkLookupKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает список всех пользователей.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список пользователей.</returns>
    Task<IReadOnlyList<ProxyUser>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет нового пользователя.
    /// </summary>
    /// <param name="user">Пользователь для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(ProxyUser user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующего пользователя.
    /// </summary>
    /// <param name="user">Актуальное состояние пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task UpdateAsync(ProxyUser user, CancellationToken cancellationToken = default);
}
