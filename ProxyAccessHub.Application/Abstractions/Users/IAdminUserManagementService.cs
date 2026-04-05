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
    /// <param name="onlyManualHandling">Флаг отбора только пользователей с ручной обработкой.</param>
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
    /// Назначает пользователю trial-тариф.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="trialTariffId">Идентификатор trial-тарифа.</param>
    /// <param name="trialDurationDays">Длительность trial в днях.</param>
    /// <param name="nextTariffId">Идентификатор тарифа после окончания trial.</param>
    /// <param name="comment">Комментарий к назначению.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task AssignTrialAsync(
        Guid userId,
        Guid trialTariffId,
        int trialDurationDays,
        Guid nextTariffId,
        string? comment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт пользователя через административный интерфейс.
    /// </summary>
    /// <param name="telemtUserId">Идентификатор пользователя в telemt.</param>
    /// <param name="serverId">Идентификатор выбранного сервера.</param>
    /// <param name="tariffId">Идентификатор назначаемого тарифа.</param>
    /// <param name="customPeriodPriceRub">Индивидуальная цена периода в рублях.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task CreateUserAsync(
        string telemtUserId,
        Guid serverId,
        Guid tariffId,
        decimal? customPeriodPriceRub,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Активирует пользователя в telemt по локально сохранённому сроку доступа.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Деактивирует пользователя в telemt.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Помечает ручную обработку пользователя как завершённую.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task CompleteManualHandlingAsync(Guid userId, CancellationToken cancellationToken = default);
}
