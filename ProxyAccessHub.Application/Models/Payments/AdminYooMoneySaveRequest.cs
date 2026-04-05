namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Запрос на сохранение настроек ЮMoney.
/// </summary>
public sealed record AdminYooMoneySaveRequest(
    string Receiver,
    string NotificationSecret,
    string SuccessUrl,
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc);
