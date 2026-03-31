namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Набор proxy-ссылок пользователя, возвращённый telemt.
/// </summary>
/// <param name="Classic">Классическая ссылка.</param>
/// <param name="Secure">Secure-ссылка.</param>
/// <param name="Tls">TLS-ссылка.</param>
public sealed record TelemtUserLinks(
    string? Classic,
    string? Secure,
    string? Tls);
