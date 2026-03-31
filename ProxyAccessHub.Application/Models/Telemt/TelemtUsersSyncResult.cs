namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Результат одного прохода синхронизации пользователей telemt.
/// </summary>
/// <param name="ServerCode">Код синхронизированного сервера.</param>
/// <param name="Revision">Ревизия telemt, использованная при синхронизации.</param>
/// <param name="CreatedUsers">Количество созданных пользователей.</param>
/// <param name="UpdatedUsers">Количество обновлённых пользователей.</param>
/// <param name="ProcessedUsers">Общее количество обработанных пользователей.</param>
/// <param name="SyncedAtUtc">Дата завершения синхронизации в UTC.</param>
public sealed record TelemtUsersSyncResult(
    string ServerCode,
    string Revision,
    int CreatedUsers,
    int UpdatedUsers,
    int ProcessedUsers,
    int MarkedForManualHandlingUsers,
    DateTimeOffset SyncedAtUtc);
