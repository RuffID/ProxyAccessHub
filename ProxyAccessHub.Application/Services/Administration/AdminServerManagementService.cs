using System.Net;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Models.Administration;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Administration;

/// <summary>
/// Реализует административное управление серверами.
/// </summary>
public class AdminServerManagementService(
    IProxyAccessHubUnitOfWork unitOfWork,
    IHttpClientFactory httpClientFactory) : IAdminServerManagementService
{
    private const int API_TIMEOUT_SECONDS = 10;

    /// <inheritdoc />
    public async Task<AdminServersPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        IReadOnlyList<AdminServerListItem> items = servers
            .OrderByDescending(server => server.IsActive)
            .ThenBy(server => server.Name, StringComparer.OrdinalIgnoreCase)
            .Select(server => new AdminServerListItem(
                server.Id,
                server.Name,
                server.Host,
                server.ApiPort,
                server.ApiBearerToken,
                server.MaxUsers,
                server.IsActive))
            .ToArray();

        return new AdminServersPageData(items);
    }

    /// <inheritdoc />
    public async Task CreateAsync(string name, string host, int apiPort, string apiBearerToken, int maxUsers, bool isActive, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        ValidateServer(name, host, apiPort, apiBearerToken, maxUsers, servers, null);

        ProxyServer server = new(
            Guid.NewGuid(),
            $"srv-{Guid.NewGuid():N}",
            name.Trim(),
            host.Trim(),
            apiPort,
            NormalizeApiBearerToken(apiBearerToken),
            maxUsers,
            isActive);

        await unitOfWork.Servers.AddAsync(server, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, string name, string host, int apiPort, string apiBearerToken, int maxUsers, bool isActive, CancellationToken cancellationToken = default)
    {
        ProxyServer currentServer = await unitOfWork.Servers.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Сервер не найден.");
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);

        ValidateServer(name, host, apiPort, apiBearerToken, maxUsers, servers, currentServer.Id);

        ProxyServer updatedServer = currentServer with
        {
            Name = name.Trim(),
            Host = host.Trim(),
            ApiPort = apiPort,
            ApiBearerToken = NormalizeApiBearerToken(apiBearerToken),
            MaxUsers = maxUsers,
            IsActive = isActive
        };

        await unitOfWork.Servers.UpdateAsync(updatedServer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CheckConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProxyServer server = await unitOfWork.Servers.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Сервер не найден.");
        Uri requestUri = BuildTelemtApiUri(server.Host, server.ApiPort);
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));

            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException($"Сервер '{server.Name}' отклонил авторизацию при проверке telemt API.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Сервер '{server.Name}' вернул код {(int)response.StatusCode} при проверке telemt API. Ответ: {responseContent}");
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException($"Не удалось подключиться к telemt API сервера '{server.Name}' по адресу '{requestUri}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProxyServer server = await unitOfWork.Servers.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Сервер не найден.");
        bool hasUsers = (await unitOfWork.Users.GetAllAsync(cancellationToken)).Any(user => user.ServerId == id);

        if (hasUsers)
        {
            throw new InvalidOperationException($"Нельзя удалить сервер '{server.Name}', потому что к нему привязан хотя бы один пользователь.");
        }

        await unitOfWork.Servers.DeleteAsync(id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateServer(
        string name,
        string host,
        int apiPort,
        string apiBearerToken,
        int maxUsers,
        IReadOnlyList<ProxyServer> existingServers,
        Guid? currentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Название сервера не задано.");
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Хост сервера не задан.");
        }

        if (!TryExtractApiEndpoint(host.Trim(), out _, out _))
        {
            throw new InvalidOperationException("Хост сервера должен быть доменным именем или IP-адресом. Разрешён формат host, host:port или полный URL.");
        }

        if (apiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API должен быть в диапазоне от 1 до 65535.");
        }

        if (string.IsNullOrWhiteSpace(apiBearerToken))
        {
            throw new InvalidOperationException("Bearer-токен API не задан.");
        }

        if (string.IsNullOrWhiteSpace(NormalizeApiBearerToken(apiBearerToken)))
        {
            throw new InvalidOperationException("Bearer-токен API не задан.");
        }

        if (maxUsers <= 0)
        {
            throw new InvalidOperationException("Лимит пользователей должен быть больше нуля.");
        }

        bool hasDuplicateName = existingServers.Any(server =>
            currentId != server.Id
            && string.Equals(server.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (hasDuplicateName)
        {
            throw new InvalidOperationException($"Сервер с названием '{name.Trim()}' уже существует.");
        }

        bool hasDuplicateEndpoint = existingServers.Any(server =>
            currentId != server.Id
            && string.Equals(server.Host.Trim(), host.Trim(), StringComparison.OrdinalIgnoreCase)
            && server.ApiPort == apiPort);

        if (hasDuplicateEndpoint)
        {
            throw new InvalidOperationException($"Сервер с адресом '{host.Trim()}:{apiPort}' уже существует.");
        }
    }

    private static Uri BuildTelemtApiUri(string host, int apiPort)
    {
        if (!TryExtractApiEndpoint(host, out string? apiScheme, out string? apiHost))
        {
            throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
        }

        if (apiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API должен быть в диапазоне от 1 до 65535.");
        }

        UriBuilder uriBuilder = new(apiScheme, apiHost, apiPort, "/v1/health");
        return uriBuilder.Uri;
    }

    private static string BuildAuthorizationHeader(string apiBearerToken)
    {
        return $"Bearer {NormalizeApiBearerToken(apiBearerToken)}";
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
