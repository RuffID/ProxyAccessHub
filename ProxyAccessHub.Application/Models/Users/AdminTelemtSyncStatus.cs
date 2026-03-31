namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Данные виджета фоновой синхронизации telemt для админки.
/// </summary>
/// <param name="StatusName">Отображаемый статус синхронизации.</param>
/// <param name="StatusMessage">Подробности текущего статуса.</param>
/// <param name="LastSuccessfulSyncAtUtc">Время последней успешной синхронизации в UTC.</param>
/// <param name="LastFailedSyncAtUtc">Время последней неуспешной синхронизации в UTC.</param>
/// <param name="Revision">Последняя успешная ревизия telemt.</param>
/// <param name="ProcessedUsers">Количество обработанных пользователей в последнем успешном проходе.</param>
/// <param name="CreatedUsers">Количество созданных пользователей в последнем успешном проходе.</param>
/// <param name="UpdatedUsers">Количество обновлённых пользователей в последнем успешном проходе.</param>
/// <param name="MarkedForManualHandlingUsers">Количество пользователей, переведённых в ручную обработку из-за расхождений.</param>
public sealed record AdminTelemtSyncStatus(
    string StatusName,
    string StatusMessage,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastFailedSyncAtUtc,
    string? Revision,
    int? ProcessedUsers,
    int? CreatedUsers,
    int? UpdatedUsers,
    int? MarkedForManualHandlingUsers);
