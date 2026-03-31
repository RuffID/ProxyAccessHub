namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Данные страницы пользователей для минимальной админки.
/// </summary>
/// <param name="Users">Пользователи для таблицы.</param>
/// <param name="Tariffs">Тарифы, доступные для изменения.</param>
public sealed record AdminUsersPageData(
    IReadOnlyList<AdminUserListItem> Users,
    IReadOnlyList<AdminTariffOption> Tariffs,
    AdminTelemtSyncStatus TelemtSyncStatus);
