using ProxyAccessHub.Application.Models.Administration;

namespace ProxyAccessHub.Application.Abstractions.Administration;

/// <summary>
/// Управляет тарифами из административного интерфейса.
/// </summary>
public interface IAdminTariffManagementService
{
    /// <summary>
    /// Возвращает данные страницы тарифов.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные для административной страницы тарифов.</returns>
    Task<AdminTariffsPageData> GetPageDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новый тариф.
    /// </summary>
    Task CreateAsync(string name, decimal periodPriceRub, int periodMonths, bool isActive, bool isDefault, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет существующий тариф.
    /// </summary>
    Task UpdateAsync(Guid id, string name, decimal periodPriceRub, int periodMonths, bool isActive, bool isDefault, CancellationToken cancellationToken = default);
}
