using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Telemt;

/// <summary>
/// Клиент чтения данных из telemt API.
/// </summary>
public interface ITelemtApiClient
{
    /// <summary>
    /// Возвращает список пользователей telemt вместе с ревизией конфигурации.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Снимок пользователей telemt.</returns>
    Task<TelemtUsersSnapshot> GetUsersAsync(ProxyServer server, CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт нового пользователя в telemt.
    /// </summary>
    /// <param name="username">Идентификатор нового пользователя в telemt.</param>
    /// <param name="expirationUtc">Дата окончания оплаченного доступа в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат создания пользователя в telemt.</returns>
    Task<TelemtCreatedUserResult> CreateUserAsync(
        ProxyServer server,
        string username,
        DateTimeOffset expirationUtc,
        int maxTcpConnections,
        int maxUniqueIps,
        CancellationToken cancellationToken = default);
}
