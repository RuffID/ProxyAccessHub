using System.Globalization;
using System.Text.Json;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Services.Administration;

/// <summary>
/// Реализует административную настройку OAuth-доступа к ЮMoney.
/// </summary>
public class AdminYooMoneyManagementService(
    IYooMoneySettingsStore yooMoneySettingsStore,
    IHttpClientFactory httpClientFactory) : IAdminYooMoneyManagementService
{
    private const string YOOMONEY_SCOPE = "account-info operation-history";
    private static readonly Uri TOKEN_ENDPOINT_URI = new("https://yoomoney.ru/oauth/token");

    /// <inheritdoc />
    public async Task<AdminYooMoneyPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
    {
        YooMoneySettingsSnapshot settings = await yooMoneySettingsStore.GetAsync(cancellationToken);
        bool hasAccessToken = !string.IsNullOrWhiteSpace(settings.AccessToken);
        bool isExpired = settings.AccessTokenExpiresAtUtc is not null
            && settings.AccessTokenExpiresAtUtc.Value <= DateTimeOffset.UtcNow;
        string statusName = GetStatusName(hasAccessToken, isExpired, settings.AccessTokenExpiresAtUtc);

        return new AdminYooMoneyPageData(
            settings.Receiver,
            settings.NotificationSecret,
            settings.SuccessUrl,
            settings.ClientId,
            settings.ClientSecret,
            settings.RedirectUri,
            settings.AccessToken,
            settings.AccessTokenExpiresAtUtc,
            hasAccessToken && !isExpired,
            isExpired,
            statusName);
    }

    /// <inheritdoc />
    public async Task SaveSettingsAsync(AdminYooMoneySaveRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequiredText(request.Receiver, "Receiver");
        ValidateRequiredText(request.NotificationSecret, "NotificationSecret");
        ValidateAbsoluteUri(request.SuccessUrl, "SuccessUrl");

        if (!string.IsNullOrWhiteSpace(request.ClientId)
            || !string.IsNullOrWhiteSpace(request.ClientSecret)
            || !string.IsNullOrWhiteSpace(request.RedirectUri)
            || !string.IsNullOrWhiteSpace(request.AccessToken)
            || request.AccessTokenExpiresAtUtc is not null)
        {
            ValidateRequiredText(request.ClientId, "ClientId");
            ValidateRequiredText(request.ClientSecret, "ClientSecret");
            ValidateAbsoluteUri(request.RedirectUri, "RedirectUri");

            if (string.IsNullOrWhiteSpace(request.AccessToken) != (request.AccessTokenExpiresAtUtc is null))
            {
                throw new InvalidOperationException("AccessToken и дата окончания действия токена должны быть заполнены вместе.");
            }
        }

        YooMoneySettingsSnapshot settings = new(
            request.Receiver.Trim(),
            request.NotificationSecret.Trim(),
            request.SuccessUrl.Trim(),
            request.ClientId.Trim(),
            request.ClientSecret.Trim(),
            request.RedirectUri.Trim(),
            request.AccessToken.Trim(),
            request.AccessTokenExpiresAtUtc);

        await yooMoneySettingsStore.SaveAsync(settings, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<YooMoneyAuthorizationRequest> GetAuthorizationRequestAsync(CancellationToken cancellationToken = default)
    {
        YooMoneySettingsSnapshot settings = await yooMoneySettingsStore.GetAsync(cancellationToken);
        ValidateRequiredText(settings.ClientId, "ClientId");
        ValidateRequiredText(settings.ClientSecret, "ClientSecret");
        ValidateAbsoluteUri(settings.RedirectUri, "RedirectUri");

        return new YooMoneyAuthorizationRequest(
            settings.ClientId.Trim(),
            settings.RedirectUri.Trim(),
            YOOMONEY_SCOPE,
            "code");
    }

    /// <inheritdoc />
    public async Task ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        string normalizedCode = ValidateRequiredText(code, "code");
        YooMoneySettingsSnapshot settings = await yooMoneySettingsStore.GetAsync(cancellationToken);
        ValidateRequiredText(settings.ClientId, "ClientId");
        ValidateRequiredText(settings.ClientSecret, "ClientSecret");
        ValidateAbsoluteUri(settings.RedirectUri, "RedirectUri");

        using HttpRequestMessage request = new(HttpMethod.Post, TOKEN_ENDPOINT_URI);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = normalizedCode,
            ["client_id"] = settings.ClientId.Trim(),
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = settings.RedirectUri.Trim(),
            ["client_secret"] = settings.ClientSecret.Trim()
        });

        HttpClient httpClient = httpClientFactory.CreateClient();
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ЮMoney вернул код {(int)response.StatusCode} при обмене OAuth-кода на токен. Ответ: {responseContent}");
        }

        string accessToken = ParseAccessToken(responseContent);
        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddYears(3);
        YooMoneySettingsSnapshot updatedSettings = settings with
        {
            AccessToken = accessToken,
            AccessTokenExpiresAtUtc = expiresAtUtc
        };

        await yooMoneySettingsStore.SaveAsync(updatedSettings, cancellationToken);
    }

    private static string ParseAccessToken(string responseContent)
    {
        using JsonDocument document = JsonDocument.Parse(responseContent);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement errorElement))
        {
            string error = errorElement.GetString() ?? "unknown_error";
            string errorDescription = root.TryGetProperty("error_description", out JsonElement descriptionElement)
                ? descriptionElement.GetString() ?? string.Empty
                : string.Empty;
            string message = string.IsNullOrWhiteSpace(errorDescription) ? error : $"{error}: {errorDescription}";
            throw new InvalidOperationException($"ЮMoney вернул ошибку при выдаче токена: {message}");
        }

        if (!root.TryGetProperty("access_token", out JsonElement accessTokenElement) || accessTokenElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("В ответе ЮMoney отсутствует access_token.");
        }

        string? accessToken = accessTokenElement.GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("В ответе ЮMoney пришёл пустой access_token.");
        }

        return accessToken.Trim();
    }

    private static string GetStatusName(bool hasAccessToken, bool isExpired, DateTimeOffset? accessTokenExpiresAtUtc)
    {
        if (!hasAccessToken)
        {
            return "Не подключено";
        }

        if (isExpired)
        {
            return $"Токен истёк {accessTokenExpiresAtUtc!.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)} UTC";
        }

        return accessTokenExpiresAtUtc is null
            ? "Подключено"
            : $"Подключено до {accessTokenExpiresAtUtc.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)} UTC";
    }

    private static string ValidateRequiredText(string value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"В настройках ЮMoney не задано значение '{settingName}'.");
        }

        return value.Trim();
    }

    private static void ValidateAbsoluteUri(string value, string settingName)
    {
        string normalizedValue = ValidateRequiredText(value, settingName);
        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException($"Значение '{settingName}' должно быть абсолютным URL.");
        }
    }
}
