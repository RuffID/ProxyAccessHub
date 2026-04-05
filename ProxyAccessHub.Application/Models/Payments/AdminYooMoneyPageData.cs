namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Данные административной страницы настройки ЮMoney.
/// </summary>
public sealed record AdminYooMoneyPageData(
    string Receiver,
    string NotificationSecret,
    string SuccessUrl,
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    bool IsConnected,
    bool IsAccessTokenExpired,
    string StatusName);
