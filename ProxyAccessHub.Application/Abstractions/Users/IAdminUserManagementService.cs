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
    /// Обновляет индивидуальную цену периода для пользователя.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="customPeriodPriceRub">Индивидуальная цена периода в рублях.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task UpdateUserTariffPriceAsync(
        Guid userId,
        decimal customPeriodPriceRub,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет назначенный тариф пользователя.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="tariffId">Идентификатор нового тарифа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task UpdateUserTariffAsync(
        Guid userId,
        Guid tariffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает ручную обработку пользователя как завершённую.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task CompleteManualHandlingAsync(Guid userId, CancellationToken cancellationToken = default);
}
