namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Снимок списка пользователей telemt.
/// </summary>
/// <param name="Revision">Ревизия конфигурации telemt.</param>
/// <param name="Users">Пользователи, полученные от telemt.</param>
public sealed record TelemtUsersSnapshot(
    string Revision,
    IReadOnlyList<TelemtUserSnapshot> Users);
