using ProxyAccessHub.Application.Models.Telemt;

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
    Task<TelemtUsersSnapshot> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт нового пользователя в telemt.
    /// </summary>
    /// <param name="username">Идентификатор нового пользователя в telemt.</param>
    /// <param name="expirationUtc">Дата окончания оплаченного доступа в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат создания пользователя в telemt.</returns>
    Task<TelemtCreatedUserResult> CreateUserAsync(
        string username,
        DateTimeOffset expirationUtc,
        CancellationToken cancellationToken = default);
}
