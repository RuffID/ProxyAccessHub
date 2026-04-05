using System.Net;
using System.Text;
using System.Text.Json;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Telemt.Models;

namespace ProxyAccessHub.Infrastructure.Telemt;

/// <summary>
/// HTTP-клиент чтения и обновления данных через telemt API.
/// </summary>
public class TelemtApiClient(IHttpClientFactory httpClientFactory) : ITelemtApiClient
{
    private const int API_TIMEOUT_SECONDS = 180;

    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<TelemtUsersSnapshot> GetUsersAsync(ProxyServer server, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = CreateHttpClient();
        using HttpRequestMessage request = new(HttpMethod.Get, BuildUsersUri(server));
        request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Telemt API не вернул список пользователей сервера '{server.Name}'. Код ответа: {(int)response.StatusCode}. Ответ: {json}");
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Telemt API вернул пустой JSON-ответ.");
        }

        TelemtSuccessEnvelope<List<TelemtUserResponse>>? envelope = JsonSerializer.Deserialize<TelemtSuccessEnvelope<List<TelemtUserResponse>>>(json, JSON_OPTIONS);

        if (envelope is null)
        {
            throw new InvalidOperationException("Telemt API вернул пустой JSON-ответ.");
        }

        if (!envelope.Ok)
        {
            throw new InvalidOperationException("Telemt API вернул неуспешный ответ для списка пользователей.");
        }

        if (envelope.Data is null)
        {
            throw new InvalidOperationException("Telemt API не вернул список пользователей.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Revision))
        {
            throw new InvalidOperationException("Telemt API не вернул ревизию конфигурации.");
        }

        IReadOnlyList<TelemtUserSnapshot> users = envelope.Data.Select(Map).ToArray();
        return new TelemtUsersSnapshot(envelope.Revision.Trim(), users);
    }

    /// <inheritdoc />
    public async Task<TelemtCreatedUserResult> CreateUserAsync(
        ProxyServer server,
        string username,
        DateTimeOffset expirationUtc,
        int maxTcpConnections,
        int maxUniqueIps,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Не задан идентификатор нового пользователя telemt.");
        }

        if (maxTcpConnections <= 0)
        {
            throw new InvalidOperationException("Лимит TCP-подключений должен быть больше нуля.");
        }

        if (maxUniqueIps <= 0)
        {
            throw new InvalidOperationException("Лимит уникальных IP должен быть больше нуля.");
        }

        HttpClient httpClient = CreateHttpClient();
        using HttpRequestMessage request = new(HttpMethod.Post, BuildUsersUri(server));
        request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new TelemtCreateUserRequest
            {
                Username = username.Trim(),
                ExpirationRfc3339 = expirationUtc.UtcDateTime.ToString("O"),
                MaxTcpConnections = maxTcpConnections,
                MaxUniqueIps = maxUniqueIps
            }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Telemt API не создал пользователя '{username.Trim()}'. Код ответа: {(int)response.StatusCode}. {responseContent}");
        }

        TelemtSuccessEnvelope<TelemtCreateUserResponseData>? envelope = JsonSerializer.Deserialize<TelemtSuccessEnvelope<TelemtCreateUserResponseData>>(responseContent, JSON_OPTIONS);
        if (envelope is null)
        {
            throw new InvalidOperationException("Telemt API вернул пустой JSON-ответ при создании пользователя.");
        }

        if (!envelope.Ok)
        {
            throw new InvalidOperationException("Telemt API вернул неуспешный ответ при создании пользователя.");
        }

        if (envelope.Data?.User is null)
        {
            throw new InvalidOperationException("Telemt API не вернул данные созданного пользователя.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Data.Secret))
        {
            throw new InvalidOperationException("Telemt API не вернул секрет созданного пользователя.");
        }

        if (string.IsNullOrWhiteSpace(envelope.Revision))
        {
            throw new InvalidOperationException("Telemt API не вернул ревизию конфигурации после создания пользователя.");
        }

        return new TelemtCreatedUserResult(
            envelope.Revision.Trim(),
            Map(envelope.Data.User),
            envelope.Data.Secret.Trim());
    }

    /// <inheritdoc />
    public async Task UpdateUserExpirationAsync(
        ProxyServer server,
        string username,
        DateTimeOffset expirationUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Не задан идентификатор пользователя telemt для обновления срока действия.");
        }

        HttpClient httpClient = CreateHttpClient();
        using HttpRequestMessage request = new(HttpMethod.Patch, BuildUserUri(server, username.Trim()));
        request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                expiration_rfc3339 = expirationUtc.UtcDateTime.ToString("O")
            }),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Telemt API не обновил срок действия пользователя '{username.Trim()}' на сервере '{server.Name}'. Код ответа: {(int)response.StatusCode}. Ответ: {responseContent}");
        }
    }

    private static TelemtUserSnapshot Map(TelemtUserResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Username))
        {
            throw new InvalidOperationException("Telemt API вернул пользователя без username.");
        }

        TelemtLinksResponse? links = response.Links ?? throw new InvalidOperationException($"Telemt API не вернул links для пользователя '{response.Username}'.");

        return new TelemtUserSnapshot(
            response.Username.Trim(),
            string.IsNullOrWhiteSpace(response.UserAdTag) ? null : response.UserAdTag.Trim(),
            response.MaxTcpConnections,
            response.ExpirationUtc,
            response.DataQuotaBytes,
            response.MaxUniqueIps,
            response.CurrentConnections,
            response.ActiveUniqueIps,
            response.TotalOctets,
            new TelemtUserLinks(links.Classic, links.Secure, links.Tls));
    }

    private HttpClient CreateHttpClient()
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);
        return httpClient;
    }

    private static Uri BuildUsersUri(ProxyServer server)
    {
        if (!TryExtractApiEndpoint(server.Host, out string? apiScheme, out string? apiHost))
        {
            throw new InvalidOperationException($"Хост сервера '{server.Host}' имеет неверный формат.");
        }

        if (server.ApiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API сервера должен быть в диапазоне от 1 до 65535.");
        }

        return new UriBuilder(apiScheme, apiHost, server.ApiPort, "/v1/users").Uri;
    }

    private static Uri BuildUserUri(ProxyServer server, string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Не задан идентификатор пользователя telemt.");
        }

        if (!TryExtractApiEndpoint(server.Host, out string? apiScheme, out string? apiHost))
        {
            throw new InvalidOperationException($"Хост сервера '{server.Host}' имеет неверный формат.");
        }

        if (server.ApiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API сервера должен быть в диапазоне от 1 до 65535.");
        }

        return new UriBuilder(apiScheme, apiHost, server.ApiPort, $"/v1/users/{Uri.EscapeDataString(username.Trim())}").Uri;
    }

    private static string BuildAuthorizationHeader(string apiBearerToken)
    {
        string normalizedToken = NormalizeApiBearerToken(apiBearerToken);

        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            throw new InvalidOperationException("Bearer-токен API сервера не задан.");
        }

        return $"Bearer {normalizedToken}";
    }

    private static string NormalizeApiBearerToken(string apiBearerToken)
    {
        string trimmedApiBearerToken = apiBearerToken.Trim();
        return trimmedApiBearerToken.StartsWith("Bearer ", StringComparison.Ordinal)
            ? trimmedApiBearerToken["Bearer ".Length..].Trim()
            : trimmedApiBearerToken;
    }

    private static bool TryExtractApiEndpoint(string host, out string? apiScheme, out string? apiHost)
    {
        apiScheme = null;
        apiHost = null;

        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        string trimmedHost = host.Trim();

        if (Uri.TryCreate(trimmedHost, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (string.IsNullOrWhiteSpace(absoluteUri.Host))
            {
                return false;
            }

            UriHostNameType absoluteHostNameType = Uri.CheckHostName(absoluteUri.Host);

            if (absoluteHostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
            {
                return false;
            }

            apiScheme = absoluteUri.Scheme;
            apiHost = absoluteUri.Host;
            return true;
        }

        if (IPAddress.TryParse(trimmedHost, out _))
        {
            apiScheme = Uri.UriSchemeHttp;
            apiHost = trimmedHost;
            return true;
        }

        if (Uri.CheckHostName(trimmedHost) is UriHostNameType.Dns)
        {
            apiScheme = Uri.UriSchemeHttp;
            apiHost = trimmedHost;
            return true;
        }

        if (!Uri.TryCreate($"{Uri.UriSchemeHttp}://{trimmedHost}", UriKind.Absolute, out Uri? hostUri))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hostUri.Host))
        {
            return false;
        }

        UriHostNameType hostNameType = Uri.CheckHostName(hostUri.Host);

        if (hostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
        {
            return false;
        }

        apiScheme = Uri.UriSchemeHttp;
        apiHost = hostUri.Host;
        return true;
    }
}
