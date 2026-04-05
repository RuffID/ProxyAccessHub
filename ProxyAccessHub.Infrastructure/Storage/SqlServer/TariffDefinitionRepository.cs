using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.Abstractions.Database.Repository.Base;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Tariffs;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Data.Entities;

namespace ProxyAccessHub.Infrastructure.Storage.SqlServer;

/// <summary>
/// Репозиторий тарифов на базе SQL Server.
/// </summary>
public class TariffDefinitionRepository(
    IAppDbContext<ProxyAccessHubDbContext> dbContext,
    IGetItemByPredicateRepository<TariffDefinitionEntity, ProxyAccessHubDbContext> getByPredicateRepository,
    IQueryRepository<TariffDefinitionEntity, ProxyAccessHubDbContext> queryRepository,
    ICreateItemRepository<TariffDefinitionEntity, ProxyAccessHubDbContext> createRepository) : ITariffDefinitionRepository
{
    /// <inheritdoc />
    public async Task<TariffDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор тарифа не задан.");
        }

        TariffDefinitionEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            tariff => tariff.Id == id,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<TariffDefinition?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        TariffDefinitionEntity? entity = await getByPredicateRepository.GetItemByPredicateAsync(
            tariff => tariff.IsDefault,
            asNoTracking: true,
            ct: cancellationToken);

        return entity is null ? null : Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TariffDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<TariffDefinitionEntity> entities = await queryRepository.Query(asNoTracking: true)
            .OrderBy(tariff => tariff.Name)
            .ThenBy(tariff => tariff.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public Task AddAsync(TariffDefinition tariff, CancellationToken cancellationToken = default)
    {
        ValidateTariff(tariff);
        cancellationToken.ThrowIfCancellationRequested();
        createRepository.Create(Map(tariff));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(TariffDefinition tariff, CancellationToken cancellationToken = default)
    {
        ValidateTariff(tariff);
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Set<TariffDefinitionEntity>().Update(Map(tariff));
        return Task.CompletedTask;
    }

    private static TariffDefinitionEntity Map(TariffDefinition tariff)
    {
        return new TariffDefinitionEntity
        {
            Id = tariff.Id,
            Name = tariff.Name,
            PeriodPriceRub = tariff.PeriodPriceRub,
            PeriodMonths = tariff.PeriodMonths,
            IsUnlimited = tariff.IsUnlimited,
            RequiresRenewal = tariff.RequiresRenewal,
            IsActive = tariff.IsActive,
            IsDefault = tariff.IsDefault
        };
    }

    private static TariffDefinition Map(TariffDefinitionEntity entity)
    {
        return new TariffDefinition(
            entity.Id,
            entity.Name,
            entity.PeriodPriceRub,
            entity.PeriodMonths,
            entity.IsUnlimited,
            entity.RequiresRenewal,
            entity.IsActive,
            entity.IsDefault);
    }

    private static void ValidateTariff(TariffDefinition tariff)
    {
        if (tariff.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Идентификатор тарифа не задан.");
        }

        if (string.IsNullOrWhiteSpace(tariff.Name))
        {
            throw new InvalidOperationException("Название тарифа не задано.");
        }

        if (tariff.PeriodPriceRub < 0m)
        {
            throw new InvalidOperationException("Стоимость тарифа не может быть отрицательной.");
        }

        if (tariff.RequiresRenewal && !TariffPeriodHelper.IsSupported(tariff.PeriodMonths))
        {
            throw new InvalidOperationException("Срок действия тарифа должен быть больше нуля.");
        }

        if (tariff.RequiresRenewal && TariffPeriodHelper.IsUnlimited(tariff.PeriodMonths))
        {
            throw new InvalidOperationException("РЎСЂРѕРє РґРµР№СЃС‚РІРёСЏ С‚Р°СЂРёС„Р° РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ Р±РѕР»СЊС€Рµ РЅСѓР»СЏ.");
        }

        if (!tariff.RequiresRenewal && tariff.PeriodMonths != 0)
        {
            throw new InvalidOperationException("Бессрочный тариф должен иметь срок 0 месяцев.");
        }

        if (!tariff.RequiresRenewal && tariff.PeriodPriceRub != 0m)
        {
            throw new InvalidOperationException("Бессрочный тариф должен иметь стоимость 0 рублей.");
        }

        if (tariff.IsUnlimited != !tariff.RequiresRenewal)
        {
            throw new InvalidOperationException("Признаки бессрочности тарифа заданы неконсистентно.");
        }
    }
}
