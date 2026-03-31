namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Сервер, на котором размещаются пользовательские подключения.
/// </summary>
/// <param name="Id">Локальный идентификатор сервера.</param>
/// <param name="Code">Код сервера.</param>
/// <param name="Name">Отображаемое название сервера.</param>
/// <param name="Host">Хост или адрес сервера.</param>
/// <param name="MaxUsers">Лимит пользователей на сервере.</param>
public record ProxyServer(
    Guid Id,
    string Code,
    string Name,
    string Host,
    int MaxUsers);
