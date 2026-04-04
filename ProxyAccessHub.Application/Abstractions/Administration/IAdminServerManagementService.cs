using ProxyAccessHub.Application.Models.Administration;

namespace ProxyAccessHub.Application.Abstractions.Administration;

/// <summary>
/// Управляет серверами из административного интерфейса.
/// </summary>
public interface IAdminServerManagementService
{
    /// <summary>
    /// Возвращает данные страницы серверов.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные для административной страницы серверов.</returns>
    Task<AdminServersPageData> GetPageDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новый сервер.
    /// </summary>
    Task CreateAsync(string name, string host, int apiPort, string apiBearerToken, int maxUsers, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующий сервер.
    /// </summary>
    Task UpdateAsync(Guid id, string name, string host, int apiPort, string apiBearerToken, int maxUsers, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет доступность сервера по сохранённым настройкам telemt API.
    /// </summary>
    /// <param name="id">Идентификатор сервера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task CheckConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет сервер, если к нему не привязаны пользователи.
    /// </summary>
    /// <param name="id">Идентификатор сервера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
