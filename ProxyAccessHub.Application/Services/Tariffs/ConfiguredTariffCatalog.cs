using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Tariffs;

namespace ProxyAccessHub.Application.Services.Tariffs;

/// <summary>
/// Преобразует тарифы из конфигурации в нормализованный каталог приложения.
/// </summary>
public sealed class ConfiguredTariffCatalog : ITariffCatalog
{
    private readonly IReadOnlyDictionary<string, TariffPlan> tariffsByCode;
    private readonly IReadOnlyList<TariffPlan> tariffs;

    /// <summary>
    /// Инициализирует каталог тарифов из конфигурации.
    /// </summary>
    /// <param name="options">Конфигурация тарифов.</param>
    public ConfiguredTariffCatalog(IOptions<TariffCatalogOptions> options)
    {
        TariffCatalogOptions catalogOptions = options.Value;
        string defaultTariffCode = RequireCode(catalogOptions.DefaultTariffCode, nameof(catalogOptions.DefaultTariffCode));

        if (catalogOptions.Items.Count == 0)
        {
            throw new InvalidOperationException("Каталог тарифов пуст.");
        }

        Dictionary<string, TariffPlan> buffer = new(StringComparer.OrdinalIgnoreCase);
        List<TariffPlan> items = new(catalogOptions.Items.Count);

        foreach (TariffDefinitionOptions item in catalogOptions.Items)
        {
            TariffPlan tariff = CreateTariff(item);

            if (!buffer.TryAdd(tariff.Code, tariff))
            {
                throw new InvalidOperationException($"Тариф с кодом '{tariff.Code}' объявлен повторно.");
            }

            items.Add(tariff);
        }

        if (!buffer.TryGetValue(defaultTariffCode, out TariffPlan? defaultTariff))
        {
            throw new InvalidOperationException($"Тариф по умолчанию '{defaultTariffCode}' не найден в каталоге.");
        }

        tariffsByCode = buffer;
        tariffs = items;
        DefaultTariff = defaultTariff;
    }

    /// <inheritdoc />
    public TariffPlan DefaultTariff { get; }

    /// <inheritdoc />
    public IReadOnlyList<TariffPlan> GetAll()
    {
        return tariffs;
    }

    /// <inheritdoc />
    public TariffPlan GetRequired(string code)
    {
        string normalizedCode = RequireCode(code, nameof(code));

        if (!tariffsByCode.TryGetValue(normalizedCode, out TariffPlan? tariff))
        {
            throw new KeyNotFoundException($"Тариф с кодом '{normalizedCode}' не найден.");
        }

        return tariff;
    }

    private static TariffPlan CreateTariff(TariffDefinitionOptions options)
    {
        string code = RequireCode(options.Code, nameof(options.Code));

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new InvalidOperationException($"Для тарифа '{code}' не задано название.");
        }

        if (options.PeriodMonths <= 0)
        {
            throw new InvalidOperationException($"Для тарифа '{code}' длительность периода должна быть больше нуля.");
        }

        if (options.RequiresRenewal && options.PeriodPriceRub <= 0m)
        {
            throw new InvalidOperationException($"Для тарифа '{code}' цена периода должна быть больше нуля.");
        }

        if (!options.RequiresRenewal && options.PeriodPriceRub < 0m)
        {
            throw new InvalidOperationException($"Для тарифа '{code}' цена периода не может быть отрицательной.");
        }

        if (options.IsUnlimited && options.RequiresRenewal)
        {
            throw new InvalidOperationException($"Безлимитный тариф '{code}' не должен требовать продления.");
        }

        return new TariffPlan(
            code,
            options.Name.Trim(),
            options.PeriodPriceRub,
            options.PeriodMonths,
            options.IsUnlimited,
            options.RequiresRenewal);
    }

    private static string RequireCode(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Не задано значение '{paramName}'.");
        }

        return value.Trim();
    }
}
