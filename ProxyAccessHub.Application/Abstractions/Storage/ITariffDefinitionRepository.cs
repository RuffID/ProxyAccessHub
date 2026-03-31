using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий тарифов, доступных для назначения пользователям.
/// </summary>
public interface ITariffDefinitionRepository
{
    /// <summary>
    /// Возвращает тариф по коду.
    /// </summary>
    /// <param name="code">Код тарифа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный тариф или <see langword="null" />.</returns>
    Task<TariffDefinition?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все тарифы.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список тарифов.</returns>
    Task<IReadOnlyList<TariffDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
}
