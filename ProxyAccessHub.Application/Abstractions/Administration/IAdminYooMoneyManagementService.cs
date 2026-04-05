using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Administration;

/// <summary>
/// Управляет административной авторизацией и настройками ЮMoney.
/// </summary>
public interface IAdminYooMoneyManagementService
{
    /// <summary>
    /// Возвращает данные страницы ЮMoney.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные страницы ЮMoney.</returns>
    Task<AdminYooMoneyPageData> GetPageDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Сохраняет настройки ЮMoney.
    /// </summary>
    /// <param name="request">Запрос на сохранение настроек.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SaveSettingsAsync(AdminYooMoneySaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Подготавливает данные OAuth-запроса авторизации.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные для отправки пользователя в ЮMoney.</returns>
    Task<YooMoneyAuthorizationRequest> GetAuthorizationRequestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Обменивает временный код OAuth на токен и сохраняет его.
    /// </summary>
    /// <param name="code">Временный OAuth-код авторизации.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
