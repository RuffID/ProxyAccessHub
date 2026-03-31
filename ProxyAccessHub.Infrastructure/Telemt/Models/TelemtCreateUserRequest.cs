using System.Text.Json.Serialization;

namespace ProxyAccessHub.Infrastructure.Telemt.Models;

/// <summary>
/// Тело запроса на создание пользователя в telemt.
/// </summary>
internal sealed class TelemtCreateUserRequest
{
    /// <summary>
    /// Идентификатор нового пользователя.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Дата окончания оплаченного доступа в RFC3339.
    /// </summary>
    [JsonPropertyName("expiration_rfc3339")]
    public string ExpirationRfc3339 { get; set; } = string.Empty;
}
