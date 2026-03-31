using ProxyAccessHub.Application.Models.Tariffs;

namespace ProxyAccessHub.Application.Abstractions.Tariffs;

/// <summary>
/// Предоставляет доступ к каталогу тарифов, загруженному в приложение.
/// </summary>
public interface ITariffCatalog
{
    /// <summary>
    /// Тариф по умолчанию.
    /// </summary>
    TariffPlan DefaultTariff { get; }

    /// <summary>
    /// Возвращает все тарифы каталога.
    /// </summary>
    /// <returns>Список тарифов в памяти приложения.</returns>
    IReadOnlyList<TariffPlan> GetAll();

    /// <summary>
    /// Возвращает тариф по коду или выбрасывает исключение.
    /// </summary>
    /// <param name="code">Код тарифа.</param>
    /// <returns>Найденный тариф.</returns>
    TariffPlan GetRequired(string code);
}
