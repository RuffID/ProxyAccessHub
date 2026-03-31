using HttpClientLibrary;
using HttpClientLibrary.Abstractions;
using HttpClientLibrary.Exceptions;
using System.Net;
using System.Text.Json;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Telemt;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Infrastructure.Telemt.Models;

namespace ProxyAccessHub.Infrastructure.Telemt;

/// <summary>
/// HTTP-клиент чтения данных из telemt API.
/// </summary>
public class TelemtApiClient : ITelemtApiClient
{
    private static readonly JsonSerializerOptions JSON_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpApiClient httpApiClient;
    private readonly TelemtOptions telemtOptions;

    /// <summary>
    /// Инициализирует HTTP-клиент telemt API.
    /// </summary>
    public TelemtApiClient(IHttpApiClient httpApiClient, IOptions<TelemtOptions> telemtOptions)
    {
        this.httpApiClient = httpApiClient;
        this.telemtOptions = telemtOptions.Value;
    }

    /// <inheritdoc />
    public async Task<TelemtUsersSnapshot> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(telemtOptions.AuthorizationHeader))
        {
            headers = new Dictionary<string, string>
            {
                ["Authorization"] = telemtOptions.AuthorizationHeader
            };
        }

        string? json = await httpApiClient.GetAsync<string>("users", headers, cancellationToken);

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
        string username,
        DateTimeOffset expirationUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("Не задан идентификатор нового пользователя telemt.");
        }

        Dictionary<string, string>? headers = null;
        if (!string.IsNullOrWhiteSpace(telemtOptions.AuthorizationHeader))
        {
            headers = new Dictionary<string, string>
            {
                ["Authorization"] = telemtOptions.AuthorizationHeader
            };
        }

        HttpResponseResult<TelemtSuccessEnvelope<TelemtCreateUserResponseData>?> response;

        try
        {
            response = await httpApiClient.SendWithResponseAsync<TelemtSuccessEnvelope<TelemtCreateUserResponseData>?>(
                new HttpClientLibrary.HttpRequestOptions
                {
                    Method = HttpMethod.Post,
                    Url = "users",
                    Headers = headers,
                    Body = new TelemtCreateUserRequest
                    {
                        Username = username.Trim(),
                        ExpirationRfc3339 = expirationUtc.UtcDateTime.ToString("O")
                    }
                },
                cancellationToken);
        }
        catch (HttpRequestFailedException ex)
        {
            string responseSnippet = string.IsNullOrWhiteSpace(ex.ResponseSnippet)
                ? "Тело ответа отсутствует."
                : ex.ResponseSnippet.Trim();
            throw new InvalidOperationException(
                $"Telemt API не создал пользователя '{username.Trim()}'. Код ответа: {(int)ex.StatusCode}. {responseSnippet}",
                ex);
        }

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException($"Telemt API вернул код '{(int)response.StatusCode}' вместо '201' при создании пользователя.");
        }

        TelemtSuccessEnvelope<TelemtCreateUserResponseData>? envelope = response.Body;
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
}
