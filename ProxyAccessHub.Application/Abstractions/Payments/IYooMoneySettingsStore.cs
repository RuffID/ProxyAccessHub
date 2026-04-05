using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Payments;

/// <summary>
/// Предоставляет доступ к сохранённым настройкам интеграции с ЮMoney.
/// </summary>
public interface IYooMoneySettingsStore
{
    /// <summary>
    /// Возвращает актуальные настройки ЮMoney из конфигурации.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Снимок настроек ЮMoney.</returns>
    Task<YooMoneySettingsSnapshot> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет OAuth-настройки и токен ЮMoney в конфигурации.
    /// </summary>
    /// <param name="settings">Актуальные настройки для сохранения.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SaveAsync(YooMoneySettingsSnapshot settings, CancellationToken cancellationToken = default);
}
