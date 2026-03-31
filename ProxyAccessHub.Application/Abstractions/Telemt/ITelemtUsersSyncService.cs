using ProxyAccessHub.Application.Models.Telemt;

namespace ProxyAccessHub.Application.Abstractions.Telemt;

/// <summary>
/// Синхронизирует пользователей telemt в локальное хранилище.
/// </summary>
public interface ITelemtUsersSyncService
{
    /// <summary>
    /// Выполняет один проход синхронизации пользователей.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат синхронизации.</returns>
    Task<TelemtUsersSyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
