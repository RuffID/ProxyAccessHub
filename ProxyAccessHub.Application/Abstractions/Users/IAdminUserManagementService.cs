using ProxyAccessHub.Application.Models.Users;

namespace ProxyAccessHub.Application.Abstractions.Users;

/// <summary>
/// Предоставляет данные и команды для минимального управления пользователями из админки.
/// </summary>
public interface IAdminUserManagementService
{
    /// <summary>
    /// Возвращает данные для страницы списка пользователей администратора.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные для отображения списка пользователей и доступных тарифов.</returns>
    Task<AdminUsersPageData> GetPageDataAsync(bool onlyManualHandling, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет тариф пользователя и его индивидуальные настройки цены.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="tariffCode">Код нового тарифа.</param>
    /// <param name="customPeriodPriceRub">Индивидуальная фиксированная цена периода в рублях.</param>
    /// <param name="discountPercent">Индивидуальная скидка в процентах.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task UpdateUserTariffAsync(
        Guid userId,
        string tariffCode,
        decimal? customPeriodPriceRub,
        decimal? discountPercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает ручную обработку пользователя как завершённую.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task CompleteManualHandlingAsync(Guid userId, CancellationToken cancellationToken = default);
}
