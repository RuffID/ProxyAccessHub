using System.Text.Json.Serialization;

namespace ProxyAccessHub.Infrastructure.Telemt.Models;

/// <summary>
/// JSON-модель пользователя telemt.
/// </summary>
internal sealed class TelemtUserResponse
{
    /// <summary>
    /// Имя пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Служебный тег пользователя.
    /// </summary>
    [JsonPropertyName("user_ad_tag")]
    public string? UserAdTag { get; set; }

    /// <summary>
    /// Лимит TCP-подключений.
    /// </summary>
    [JsonPropertyName("max_tcp_conns")]
    public int? MaxTcpConnections { get; set; }

    /// <summary>
    /// Срок действия пользователя в UTC.
    /// </summary>
    [JsonPropertyName("expiration_rfc3339")]
    public DateTimeOffset? ExpirationUtc { get; set; }

    /// <summary>
    /// Лимит трафика в байтах.
    /// </summary>
    [JsonPropertyName("data_quota_bytes")]
    public long? DataQuotaBytes { get; set; }

    /// <summary>
    /// Лимит уникальных IP.
    /// </summary>
    [JsonPropertyName("max_unique_ips")]
    public int? MaxUniqueIps { get; set; }

    /// <summary>
    /// Текущее количество подключений.
    /// </summary>
    [JsonPropertyName("current_connections")]
    public int CurrentConnections { get; set; }

    /// <summary>
    /// Текущее количество активных уникальных IP.
    /// </summary>
    [JsonPropertyName("active_unique_ips")]
    public int ActiveUniqueIps { get; set; }

    /// <summary>
    /// Накопленный объём трафика.
    /// </summary>
    [JsonPropertyName("total_octets")]
    public long TotalOctets { get; set; }

    /// <summary>
    /// Набор proxy-ссылок пользователя.
    /// </summary>
    [JsonPropertyName("links")]
    public TelemtLinksResponse? Links { get; set; }
}
