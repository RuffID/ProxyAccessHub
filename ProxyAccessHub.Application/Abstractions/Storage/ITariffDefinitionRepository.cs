using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий тарифов, доступных для назначения пользователям.
/// </summary>
public interface ITariffDefinitionRepository
{
    /// <summary>
    /// Возвращает тариф по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор тарифа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный тариф или <see langword="null" />.</returns>
    Task<TariffDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает тариф по умолчанию.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Тариф по умолчанию или <see langword="null" />.</returns>
    Task<TariffDefinition?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все тарифы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список тарифов.</returns>
    Task<IReadOnlyList<TariffDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет тариф.
    /// </summary>
    /// <param name="tariff">Тариф для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(TariffDefinition tariff, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет тариф.
    /// </summary>
    /// <param name="tariff">Актуальное состояние тарифа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task UpdateAsync(TariffDefinition tariff, CancellationToken cancellationToken = default);
}
