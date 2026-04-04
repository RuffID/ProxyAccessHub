using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Models.Administration;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Administration;

/// <summary>
/// Реализует административное управление тарифами.
/// </summary>
public class AdminTariffManagementService(IProxyAccessHubUnitOfWork unitOfWork) : IAdminTariffManagementService
{
    private const int MONTHLY_PERIOD_MONTHS = 1;
    private const int YEARLY_PERIOD_MONTHS = 12;
    private const int UNLIMITED_PERIOD_MONTHS = 0;

    /// <inheritdoc />
    public async Task<AdminTariffsPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TariffDefinition> tariffs = await unitOfWork.Tariffs.GetAllAsync(cancellationToken);
        IReadOnlyList<AdminTariffListItem> items = tariffs
            .OrderByDescending(tariff => tariff.IsDefault)
            .ThenByDescending(tariff => tariff.IsActive)
            .ThenBy(tariff => tariff.Name, StringComparer.OrdinalIgnoreCase)
            .Select(tariff => new AdminTariffListItem(
                tariff.Id,
                tariff.Name,
                tariff.PeriodPriceRub,
                tariff.PeriodMonths,
                tariff.IsActive,
                tariff.IsDefault))
            .ToArray();

        return new AdminTariffsPageData(items);
    }

    /// <inheritdoc />
    public async Task CreateAsync(string name, decimal periodPriceRub, int periodMonths, bool isActive, bool isDefault, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TariffDefinition> tariffs = await unitOfWork.Tariffs.GetAllAsync(cancellationToken);
        ValidateTariff(name, periodPriceRub, periodMonths, isActive, isDefault, tariffs, null);

        if (isDefault)
        {
            await ResetDefaultFlagsAsync(tariffs, cancellationToken);
        }

        TariffDefinition tariff = new(
            Guid.NewGuid(),
            name.Trim(),
            decimal.Round(periodPriceRub, 2, MidpointRounding.AwayFromZero),
            periodMonths,
            IsUnlimitedPeriod(periodMonths),
            RequiresRenewal(periodMonths),
            isActive,
            isDefault);

        await unitOfWork.Tariffs.AddAsync(tariff, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Guid id, string name, decimal periodPriceRub, int periodMonths, bool isActive, bool isDefault, CancellationToken cancellationToken = default)
    {
        TariffDefinition currentTariff = await unitOfWork.Tariffs.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{id}' не найден.");
        IReadOnlyList<TariffDefinition> tariffs = await unitOfWork.Tariffs.GetAllAsync(cancellationToken);

        ValidateTariff(name, periodPriceRub, periodMonths, isActive, isDefault, tariffs, currentTariff.Id);

        if (!isDefault && currentTariff.IsDefault)
        {
            throw new InvalidOperationException("Нельзя снять признак тарифа по умолчанию. Сначала назначьте по умолчанию другой тариф.");
        }

        if (!isActive && currentTariff.IsDefault)
        {
            throw new InvalidOperationException("Нельзя деактивировать тариф по умолчанию. Сначала назначьте по умолчанию другой тариф.");
        }

        if (isDefault)
        {
            await ResetDefaultFlagsAsync(tariffs.Where(tariff => tariff.Id != currentTariff.Id).ToArray(), cancellationToken);
        }

        TariffDefinition updatedTariff = currentTariff with
        {
            Name = name.Trim(),
            PeriodPriceRub = decimal.Round(periodPriceRub, 2, MidpointRounding.AwayFromZero),
            PeriodMonths = periodMonths,
            IsUnlimited = IsUnlimitedPeriod(periodMonths),
            RequiresRenewal = RequiresRenewal(periodMonths),
            IsActive = isActive,
            IsDefault = isDefault
        };

        await unitOfWork.Tariffs.UpdateAsync(updatedTariff, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ResetDefaultFlagsAsync(IReadOnlyList<TariffDefinition> tariffs, CancellationToken cancellationToken)
    {
        foreach (TariffDefinition tariff in tariffs.Where(tariff => tariff.IsDefault))
        {
            await unitOfWork.Tariffs.UpdateAsync(tariff with { IsDefault = false }, cancellationToken);
        }
    }

    private static void ValidateTariff(
        string name,
        decimal periodPriceRub,
        int periodMonths,
        bool isActive,
        bool isDefault,
        IReadOnlyList<TariffDefinition> existingTariffs,
        Guid? currentId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Название тарифа не задано.");
        }

        if (periodPriceRub < 0m)
        {
            throw new InvalidOperationException("Стоимость тарифа не может быть отрицательной.");
        }

        if (periodMonths != MONTHLY_PERIOD_MONTHS
            && periodMonths != YEARLY_PERIOD_MONTHS
            && periodMonths != UNLIMITED_PERIOD_MONTHS)
        {
            throw new InvalidOperationException("Срок действия тарифа должен быть равен 1, 12 месяцам или значению 'Навсегда'.");
        }

        if (isDefault && !isActive)
        {
            throw new InvalidOperationException("Тариф по умолчанию должен быть активным.");
        }

        if (IsUnlimitedPeriod(periodMonths) && periodPriceRub != 0m)
        {
            throw new InvalidOperationException("Бессрочный тариф должен иметь стоимость 0 рублей.");
        }

        if (isDefault && (IsUnlimitedPeriod(periodMonths) || periodPriceRub == 0m))
        {
            throw new InvalidOperationException("Тариф по умолчанию должен оставаться платным периодическим тарифом.");
        }

        bool hasDuplicateName = existingTariffs.Any(tariff =>
            tariff.Id != currentId
            && string.Equals(tariff.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));

        if (hasDuplicateName)
        {
            throw new InvalidOperationException($"Тариф с названием '{name.Trim()}' уже существует.");
        }

        bool hasDefaultTariff = existingTariffs.Any(tariff =>
            tariff.Id != currentId
            && tariff.IsDefault
            && tariff.IsActive);

        if (!hasDefaultTariff && existingTariffs.Count > 0 && !isDefault)
        {
            throw new InvalidOperationException("В системе должен быть активный тариф по умолчанию. Сначала назначьте один из тарифов по умолчанию.");
        }
    }

    private static bool IsUnlimitedPeriod(int periodMonths)
    {
        return periodMonths == UNLIMITED_PERIOD_MONTHS;
    }

    private static bool RequiresRenewal(int periodMonths)
    {
        return !IsUnlimitedPeriod(periodMonths);
    }
}
