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
    public async Task<IReadOnlyList<ProxyServer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ProxyServerEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderBy(server => server.Code)
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
        cancellationToken.ThrowIfCancellationRequested();
        ValidateServer(server);
        dbContext.Set<ProxyServerEntity>().Update(Map(server));
        return Task.CompletedTask;
    }

    private static ProxyServerEntity Map(ProxyServer server)
    {
        return new ProxyServerEntity
        {
            Id = server.Id,
            Code = server.Code,
            Name = server.Name,
            Host = server.Host,
            MaxUsers = server.MaxUsers
        };
    }

    private static ProxyServer Map(ProxyServerEntity entity)
    {
        return new ProxyServer(entity.Id, entity.Code, entity.Name, entity.Host, entity.MaxUsers);
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

        if (server.MaxUsers <= 0)
        {
            throw new InvalidOperationException("Лимит пользователей на сервере должен быть больше нуля.");
        }
    }
}
