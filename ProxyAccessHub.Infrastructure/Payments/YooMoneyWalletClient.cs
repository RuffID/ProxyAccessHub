using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Infrastructure.Payments;

/// <summary>
/// Клиент wallet API ЮMoney для ручной сверки операций.
/// </summary>
public class YooMoneyWalletClient(
    IHttpClientFactory httpClientFactory,
    IYooMoneySettingsStore yooMoneySettingsStore) : IYooMoneyWalletClient
{
    private static readonly Uri OPERATION_HISTORY_URI = new("https://yoomoney.ru/api/operation-history");
    private const int MAX_RECORDS = 100;

    /// <inheritdoc />
    public async Task<IReadOnlyList<YooMoneyOperationHistoryItem>> GetOperationsByLabelAsync(
        string label,
        DateTimeOffset fromUtc,
        DateTimeOffset tillUtc,
        CancellationToken cancellationToken = default)
    {
        string normalizedLabel = string.IsNullOrWhiteSpace(label)
            ? throw new InvalidOperationException("Label заявки для сверки с ЮMoney не задан.")
            : label.Trim();
        YooMoneySettingsSnapshot options = await yooMoneySettingsStore.GetAsync(cancellationToken);
        ValidateWalletApiOptions(options);
        string accessToken = options.AccessToken.Trim();

        using HttpRequestMessage request = new(HttpMethod.Post, OPERATION_HISTORY_URI);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["type"] = "deposition",
            ["label"] = normalizedLabel,
            ["from"] = fromUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            ["till"] = tillUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
            ["records"] = MAX_RECORDS.ToString(CultureInfo.InvariantCulture)
        });

        HttpClient httpClient = httpClientFactory.CreateClient();
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"ЮMoney вернул код {(int)response.StatusCode} при запросе истории операций. Ответ: {responseContent}");
        }

        return ParseOperations(responseContent, normalizedLabel);
    }

    private static void ValidateWalletApiOptions(YooMoneySettingsSnapshot options)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан ClientId OAuth-приложения.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан ClientSecret OAuth-приложения.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан OAuth-токен wallet API.");
        }

        if (options.AccessTokenExpiresAtUtc is null)
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задана дата окончания действия OAuth-токена.");
        }

        if (options.AccessTokenExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                $"OAuth-токен ЮMoney истёк {options.AccessTokenExpiresAtUtc.Value:O}. Обновите AccessToken в конфигурации.");
        }
    }

    private static IReadOnlyList<YooMoneyOperationHistoryItem> ParseOperations(string responseContent, string expectedLabel)
    {
        using JsonDocument document = JsonDocument.Parse(responseContent);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement errorElement))
        {
            string error = errorElement.GetString() ?? "unknown_error";
            throw new InvalidOperationException($"ЮMoney вернул ошибку wallet API: {error}");
        }

        if (!root.TryGetProperty("operations", out JsonElement operationsElement) || operationsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("В ответе ЮMoney отсутствует массив операций.");
        }

        List<YooMoneyOperationHistoryItem> operations = [];
        foreach (JsonElement operationElement in operationsElement.EnumerateArray())
        {
            string label = GetRequiredString(operationElement, "label");
            if (!string.Equals(label, expectedLabel, StringComparison.Ordinal))
            {
                continue;
            }

            operations.Add(new YooMoneyOperationHistoryItem(
                GetRequiredString(operationElement, "operation_id"),
                GetRequiredString(operationElement, "status"),
                GetRequiredString(operationElement, "direction"),
                GetRequiredDecimal(operationElement, "amount"),
                GetRequiredDateTimeOffset(operationElement, "datetime"),
                label,
                GetOptionalString(operationElement, "title")));
        }

        return operations;
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        string value = GetOptionalString(element, propertyName);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"В ответе ЮMoney отсутствует обязательное поле '{propertyName}'.")
            : value.Trim();
    }

    private static string GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static decimal GetRequiredDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            throw new InvalidOperationException($"В ответе ЮMoney отсутствует обязательное поле '{propertyName}'.");
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out decimal numberValue))
        {
            return numberValue;
        }

        if (property.ValueKind == JsonValueKind.String
            && decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal stringValue))
        {
            return stringValue;
        }

        throw new InvalidOperationException($"Поле '{propertyName}' ответа ЮMoney имеет неверный числовой формат.");
    }

    private static DateTimeOffset GetRequiredDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"В ответе ЮMoney отсутствует обязательное поле '{propertyName}'.");
        }

        string rawValue = property.GetString() ?? string.Empty;
        return DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset value)
            ? value
            : throw new InvalidOperationException($"Поле '{propertyName}' ответа ЮMoney имеет неверный формат даты.");
    }
}
