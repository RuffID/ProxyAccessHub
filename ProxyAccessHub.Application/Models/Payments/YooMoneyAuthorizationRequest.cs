namespace ProxyAccessHub.Application.Models.Payments;

/// <summary>
/// Данные OAuth-запроса авторизации ЮMoney.
/// </summary>
public sealed record YooMoneyAuthorizationRequest(
    string ClientId,
    string RedirectUri,
    string Scope,
    string ResponseType);
