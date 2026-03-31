namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Снимок пользователя, полученный из telemt API.
/// </summary>
/// <param name="Username">Имя пользователя в telemt.</param>
/// <param name="UserAdTag">Служебный тег пользователя в telemt.</param>
/// <param name="MaxTcpConnections">Лимит TCP-подключений.</param>
/// <param name="ExpirationUtc">Срок действия пользователя в UTC.</param>
/// <param name="DataQuotaBytes">Лимит трафика в байтах.</param>
/// <param name="MaxUniqueIps">Лимит уникальных IP.</param>
/// <param name="CurrentConnections">Текущее количество подключений.</param>
/// <param name="ActiveUniqueIps">Текущее количество активных уникальных IP.</param>
/// <param name="TotalOctets">Накопленный объём трафика в октетах.</param>
/// <param name="Links">Набор proxy-ссылок пользователя.</param>
public sealed record TelemtUserSnapshot(
    string Username,
    string? UserAdTag,
    int? MaxTcpConnections,
    DateTimeOffset? ExpirationUtc,
    long? DataQuotaBytes,
    int? MaxUniqueIps,
    int CurrentConnections,
    int ActiveUniqueIps,
    long TotalOctets,
    TelemtUserLinks Links);
