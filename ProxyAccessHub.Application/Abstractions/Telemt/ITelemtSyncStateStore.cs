using ProxyAccessHub.Application.Models.Telemt;

namespace ProxyAccessHub.Application.Abstractions.Telemt;

/// <summary>
/// Хранит текущее состояние фоновой синхронизации telemt в памяти приложения.
/// </summary>
public interface ITelemtSyncStateStore
{
    /// <summary>
    /// Возвращает текущее состояние последней синхронизации.
    /// </summary>
    /// <returns>Состояние последней синхронизации.</returns>
    TelemtSyncState GetState();

    /// <summary>
    /// Сохраняет состояние успешной синхронизации.
    /// </summary>
    /// <param name="result">Результат успешного прохода синхронизации.</param>
    void SetSuccess(TelemtUsersSyncResult result);

    /// <summary>
    /// Сохраняет состояние неуспешной синхронизации.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="failedAtUtc">Момент ошибки в UTC.</param>
    void SetFailure(string errorMessage, DateTimeOffset failedAtUtc);
}
