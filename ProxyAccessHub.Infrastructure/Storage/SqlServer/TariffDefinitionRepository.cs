using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий тарифов на базе SQL Server.
/// </summary>
public class TariffDefinitionRepository(
    IGetItemByPredicateRepository<TariffDefinitionEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<TariffDefinitionEntity, ProxyAccessHubDbContext> queryRepository) : ITariffDefinitionRepository
{
    /// <inheritdoc />
    public async Task<TariffDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Код тарифа не задан.");
        }

        TariffDefinitionEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            tariff => tariff.Code == code.Trim(),
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TariffDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TariffDefinitionEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderBy(tariff => tariff.Code)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    private static TariffDefinition Map(TariffDefinitionEntity entity)
    {
        return new TariffDefinition(
            entity.Code,
            entity.Name,
            entity.PeriodPriceRub,
            entity.PeriodMonths,
            entity.IsUnlimited,
            entity.RequiresRenewal);
    }
}
