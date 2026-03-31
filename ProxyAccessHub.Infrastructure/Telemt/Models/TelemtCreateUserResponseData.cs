using System.Text.Json.Serialization;

namespace ProxyAccessHub.Infrastructure.Telemt.Models;

/// <summary>
/// Полезные данные успешного ответа telemt при создании пользователя.
/// </summary>
internal sealed class TelemtCreateUserResponseData
{
    /// <summary>
    /// Созданный пользователь.
    /// </summary>
    [JsonPropertyName("user")]
    public TelemtUserResponse? User { get; set; }

    /// <summary>
    /// Итоговый секрет пользователя.
    /// </summary>
    [JsonPropertyName("secret")]
    public string? Secret { get; set; }
}
