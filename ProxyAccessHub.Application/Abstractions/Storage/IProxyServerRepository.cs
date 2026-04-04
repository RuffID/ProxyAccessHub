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
    Task<ProxyServer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает сервер по коду.
    /// </summary>
    Task<ProxyServer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает активный сервер по хосту и порту telemt API.
    /// </summary>
    Task<ProxyServer?> GetActiveByEndpointAsync(string host, int apiPort, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все серверы.
    /// </summary>
    Task<IReadOnlyList<ProxyServer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет сервер.
    /// </summary>
    Task AddAsync(ProxyServer server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет сервер.
    /// </summary>
    Task UpdateAsync(ProxyServer server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сервер.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
