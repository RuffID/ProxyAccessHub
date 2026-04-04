namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Сервер, доступный для выбора в административных сценариях пользователей.
/// </summary>
/// <param name="Id">Идентификатор сервера.</param>
/// <param name="Name">Отображаемое название сервера.</param>
public sealed record AdminServerOption(
    Guid Id,
    string Name);
