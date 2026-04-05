using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий серверов на базе SQL Server.
/// </summary>
public class ProxyServerRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByIdRepository<ProxyServerEntity, Guid, ProxyAccessHubDbContext> getByIdRepository,
    IGetItemByPredicateRepository<ProxyServerEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<ProxyServerEntity, ProxyAccessHubDbContext> queryRepository,
    ICreateItemRepository<ProxyServerEntity, ProxyAccessHubDbContext> createRepository) : IProxyServerRepository
{
    /// <inheritdoc />
    public async Task<ProxyServer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProxyServerEntity? entity = await getByIdRepository.GetItemByIdAsync(id, asNoTracking: true, ct: cancellationToken);
        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<ProxyServer?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Код сервера не задан.");
        }

        ProxyServerEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            server => server.Code == code.Trim(),
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<ProxyServer?> GetActiveByEndpointAsync(string host, int apiPort, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Хост сервера не задан.");
        }

        if (apiPort <= 0)
        {
            throw new InvalidOperationException("Порт API сервера не задан.");
        }

        string normalizedHost = NormalizeApiHost(host);
        List<ProxyServerEntity> activeEntities = await queryRepository.Query(asNoTracking: true)
            .Where(server => server.IsActive && server.ApiPort == apiPort)
            .ToListAsync(cancellationToken);

        ProxyServer[] matchedServers = activeEntities
            .Select(Map)
            .Where(server => string.Equals(NormalizeApiHost(server.Host), normalizedHost, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return matchedServers.Length switch
        {
            0 => null,
            1 => matchedServers[0],
            _ => throw new InvalidOperationException($"Найдено несколько активных серверов для telemt API '{normalizedHost}:{apiPort}'.")
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProxyServer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ProxyServerEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderBy(server => server.Name)
            .ThenBy(server => server.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public Task AddAsync(ProxyServer server, CancellationToken cancellationToken = default)
    {
        ValidateServer(server);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(server));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(ProxyServer server, CancellationToken cancellationToken = default)
    {
        ValidateServer(server);
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Set<ProxyServerEntity>().Update(Map(server));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ProxyServerEntity? entity = await dbContext.Set<ProxyServerEntity>()
            .SingleOrDefaultAsync(server => server.Id == id, cancellationToken);

        if (entity is null)
        {
            throw new KeyNotFoundException("Сервер не найден.");
        }

        dbContext.Set<ProxyServerEntity>().Remove(entity);
    }

    private static ProxyServerEntity Map(ProxyServer server)
    {
        return new ProxyServerEntity
        {
            Id = server.Id,
            Code = server.Code,
            Name = server.Name,
            Host = server.Host,
            ApiPort = server.ApiPort,
            ApiBearerToken = server.ApiBearerToken,
            MaxUsers = server.MaxUsers,
            IsActive = server.IsActive,
            SyncEnabled = server.SyncEnabled,
            SyncIntervalMinutes = server.SyncIntervalMinutes
        };
    }

    private static ProxyServer Map(ProxyServerEntity entity)
    {
        return new ProxyServer(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Host,
            entity.ApiPort,
            entity.ApiBearerToken,
            entity.MaxUsers,
            entity.IsActive,
            entity.SyncEnabled,
            entity.SyncIntervalMinutes);
    }

    private static void ValidateServer(ProxyServer server)
    {
        if (string.IsNullOrWhiteSpace(server.Code))
        {
            throw new InvalidOperationException("Код сервера не задан.");
        }

        if (string.IsNullOrWhiteSpace(server.Name))
        {
            throw new InvalidOperationException("Название сервера не задано.");
        }

        if (string.IsNullOrWhiteSpace(server.Host))
        {
            throw new InvalidOperationException("Хост сервера не задан.");
        }

        if (server.ApiPort <= 0)
        {
            throw new InvalidOperationException("Порт API сервера не задан.");
        }

        if (string.IsNullOrWhiteSpace(server.ApiBearerToken))
        {
            throw new InvalidOperationException("Bearer-токен API сервера не задан.");
        }

        if (server.MaxUsers <= 0)
        {
            throw new InvalidOperationException("Лимит пользователей на сервере должен быть больше нуля.");
        }

        if (server.SyncIntervalMinutes <= 0)
        {
            throw new InvalidOperationException("Интервал фоновой синхронизации сервера должен быть больше нуля.");
        }
    }

    private static string NormalizeApiHost(string host)
    {
        string trimmedHost = host.Trim();

        if (Uri.TryCreate(trimmedHost, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (string.IsNullOrWhiteSpace(absoluteUri.Host))
            {
                throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
            }

            return absoluteUri.Host;
        }

        if (Uri.CheckHostName(trimmedHost) is UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            return trimmedHost;
        }

        if (!Uri.TryCreate($"{Uri.UriSchemeHttp}://{trimmedHost}", UriKind.Absolute, out Uri? hostUri))
        {
            throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
        }

        if (string.IsNullOrWhiteSpace(hostUri.Host))
        {
            throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
        }

        return hostUri.Host;
    }
}
