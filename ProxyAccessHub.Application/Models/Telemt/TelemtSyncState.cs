namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Текущее состояние фоновой синхронизации telemt.
/// </summary>
/// <param name="HasSuccessfulSync">Признак хотя бы одной успешной синхронизации.</param>
/// <param name="LastSuccessfulSyncAtUtc">Время последней успешной синхронизации в UTC.</param>
/// <param name="LastFailedSyncAtUtc">Время последней неуспешной синхронизации в UTC.</param>
/// <param name="LastErrorMessage">Сообщение последней ошибки синхронизации.</param>
/// <param name="LastResult">Последний успешный результат синхронизации.</param>
public sealed record TelemtSyncState(
    bool HasSuccessfulSync,
    DateTimeOffset? LastSuccessfulSyncAtUtc,
    DateTimeOffset? LastFailedSyncAtUtc,
    string? LastErrorMessage,
    TelemtUsersSyncResult? LastResult);
