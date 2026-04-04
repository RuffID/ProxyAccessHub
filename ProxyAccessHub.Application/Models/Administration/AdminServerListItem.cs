namespace ProxyAccessHub.Application.Models.Administration;

/// <summary>
/// Строка сервера для административной таблицы.
/// </summary>
public sealed record AdminServerListItem(
    Guid Id,
    string Name,
    string Host,
    int ApiPort,
    string ApiBearerToken,
    int MaxUsers,
    bool IsActive);
