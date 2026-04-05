namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Снимок настроек интеграции с ЮMoney, хранящийся в конфигурации.
/// </summary>
public sealed record YooMoneySettingsSnapshot(
    string Receiver,
    string NotificationSecret,
    string SuccessUrl,
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc);
