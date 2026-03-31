namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Набор proxy-ссылок пользователя, возвращённый telemt.
/// </summary>
/// <param name="Classic">Классические ссылки.</param>
/// <param name="Secure">Secure-ссылки.</param>
/// <param name="Tls">TLS-ссылки.</param>
public sealed record TelemtUserLinks(
    IReadOnlyList<string> Classic,
    IReadOnlyList<string> Secure,
    IReadOnlyList<string> Tls);
