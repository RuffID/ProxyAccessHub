namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Данные страницы пользователей для минимальной админки.
/// </summary>
/// <param name="Users">Пользователи для таблицы.</param>
/// <param name="Tariffs">Тарифы, доступные для изменения и создания.</param>
/// <param name="Servers">Активные серверы, доступные для создания пользователя.</param>
/// <param name="TelemtSyncStatus">Статус фоновой синхронизации с telemt.</param>
public sealed record AdminUsersPageData(
    IReadOnlyList<AdminUserListItem> Users,
    IReadOnlyList<AdminTariffOption> Tariffs,
    IReadOnlyList<AdminServerOption> Servers,
    AdminTelemtSyncStatus TelemtSyncStatus);
