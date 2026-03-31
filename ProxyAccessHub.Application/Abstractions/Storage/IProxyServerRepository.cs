using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий серверов пользователей.
/// </summary>
public interface IProxyServerRepository
{
    /// <summary>
    /// Возвращает сервер по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сервера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный сервер или <see langword="null" />.</returns>
    Task<ProxyServer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все серверы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список серверов.</returns>
    Task<IReadOnlyList<ProxyServer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет сервер.
    /// </summary>
    /// <param name="server">Сервер для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(ProxyServer server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет сервер.
    /// </summary>
    /// <param name="server">Актуальное состояние сервера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task UpdateAsync(ProxyServer server, CancellationToken cancellationToken = default);
}
