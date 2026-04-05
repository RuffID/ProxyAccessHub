using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Infrastructure.Configuration;

namespace ProxyAccessHub.Infrastructure.Payments;

/// <summary>
/// Сохраняет и читает секцию ЮMoney из файла конфигурации приложения.
/// </summary>
public class YooMoneySettingsStore(IHostEnvironment hostEnvironment) : IYooMoneySettingsStore
{
    private const string YOOMONEY_SECTION_NAME = "YooMoney";
    private readonly string configPath = ConfigPathResolver.Resolve(hostEnvironment.EnvironmentName, hostEnvironment.ContentRootPath);
    private readonly SemaphoreSlim syncLock = new(1, 1);

    /// <inheritdoc />
    public async Task<YooMoneySettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await syncLock.WaitAsync(cancellationToken);

        try
        {
            JsonObject root = await LoadRootAsync(cancellationToken);
            JsonObject section = GetYooMoneySection(root);

            return new YooMoneySettingsSnapshot(
                GetString(section, "Receiver"),
                GetString(section, "NotificationSecret"),
                GetString(section, "SuccessUrl"),
                GetString(section, "ClientId"),
                GetString(section, "ClientSecret"),
                GetString(section, "RedirectUri"),
                GetString(section, "AccessToken"),
                GetNullableDateTimeOffset(section, "AccessTokenExpiresAtUtc"));
        }
        finally
        {
            syncLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(YooMoneySettingsSnapshot settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await syncLock.WaitAsync(cancellationToken);

        try
        {
            JsonObject root = await LoadRootAsync(cancellationToken);
            JsonObject section = GetYooMoneySection(root);

            section["Receiver"] = settings.Receiver;
            section["NotificationSecret"] = settings.NotificationSecret;
            section["SuccessUrl"] = settings.SuccessUrl;
            section["ClientId"] = settings.ClientId;
            section["ClientSecret"] = settings.ClientSecret;
            section["RedirectUri"] = settings.RedirectUri;
            section["AccessToken"] = settings.AccessToken;
            section["AccessTokenExpiresAtUtc"] = settings.AccessTokenExpiresAtUtc?.ToString("O") ?? string.Empty;

            JsonSerializerOptions serializerOptions = new()
            {
                WriteIndented = true
            };

            string json = root.ToJsonString(serializerOptions);
            await File.WriteAllTextAsync(configPath, json, cancellationToken);
        }
        finally
        {
            syncLock.Release();
        }
    }

    private async Task<JsonObject> LoadRootAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException($"Файл конфигурации '{configPath}' не найден.");
        }

        string json = await File.ReadAllTextAsync(configPath, cancellationToken);
        JsonNode? rootNode = JsonNode.Parse(json);
        JsonObject? rootObject = rootNode as JsonObject;

        return rootObject ?? throw new InvalidOperationException("Корневой JSON-объект конфигурации имеет неверный формат.");
    }

    private static JsonObject GetYooMoneySection(JsonObject root)
    {
        JsonNode? sectionNode = root[YOOMONEY_SECTION_NAME];
        JsonObject? sectionObject = sectionNode as JsonObject;

        return sectionObject ?? throw new InvalidOperationException("В конфигурации отсутствует секция 'YooMoney'.");
    }

    private static string GetString(JsonObject section, string propertyName)
    {
        JsonNode? node = section[propertyName];
        return node?.GetValue<string>() ?? string.Empty;
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(JsonObject section, string propertyName)
    {
        string value = GetString(section, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out DateTimeOffset parsedValue)
            ? parsedValue
            : throw new InvalidOperationException($"Поле '{propertyName}' секции YooMoney имеет неверный формат даты.");
    }
}
