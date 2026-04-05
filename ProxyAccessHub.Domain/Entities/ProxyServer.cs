namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Сервер, на котором размещаются пользовательские подключения.
/// </summary>
/// <param name="Id">Локальный идентификатор сервера.</param>
/// <param name="Code">Код сервера.</param>
/// <param name="Name">Отображаемое название сервера.</param>
/// <param name="Host">Хост или адрес сервера.</param>
/// <param name="ApiPort">Порт telemt API сервера.</param>
/// <param name="ApiBearerToken">Bearer-токен telemt API сервера.</param>
/// <param name="MaxUsers">Лимит пользователей на сервере.</param>
/// <param name="IsActive">Признак активности сервера.</param>
/// <param name="SyncEnabled">Признак включённой фоновой синхронизации сервера.</param>
/// <param name="SyncIntervalMinutes">Интервал фоновой синхронизации сервера в минутах.</param>
public record ProxyServer(
    Guid Id,
    string Code,
    string Name,
    string Host,
    int ApiPort,
    string ApiBearerToken,
    int MaxUsers,
    bool IsActive,
    bool SyncEnabled,
    int SyncIntervalMinutes);
