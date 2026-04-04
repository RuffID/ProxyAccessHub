namespace ProxyAccessHub.Application.Models.Administration;

/// <summary>
/// Данные страницы серверов для административного интерфейса.
/// </summary>
public sealed record AdminServersPageData(
    IReadOnlyList<AdminServerListItem> Servers);
