using System.Text.Json.Serialization;

namespace ProxyAccessHub.Infrastructure.Telemt.Models;

/// <summary>
/// JSON-модель набора proxy-ссылок пользователя.
/// </summary>
internal sealed class TelemtLinksResponse
{
    /// <summary>
    /// Классическая ссылка.
    /// </summary>
    [JsonPropertyName("classic")]
    public string? Classic { get; set; }

    /// <summary>
    /// Secure-ссылка.
    /// </summary>
    [JsonPropertyName("secure")]
    public string? Secure { get; set; }

    /// <summary>
    /// TLS-ссылка.
    /// </summary>
    [JsonPropertyName("tls")]
    public string? Tls { get; set; }
}
