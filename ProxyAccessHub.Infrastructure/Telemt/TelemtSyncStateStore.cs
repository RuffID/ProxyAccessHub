using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Models.Telemt;

namespace ProxyAccessHub.Infrastructure.Telemt;

/// <summary>
/// Хранит в памяти текущее состояние фоновой синхронизации telemt.
/// </summary>
public sealed class TelemtSyncStateStore : ITelemtSyncStateStore
{
    private readonly object syncRoot = new();
    private TelemtSyncState state = new(
        false,
        null,
        null,
        null,
        null);

    /// <inheritdoc />
    public TelemtSyncState GetState()
    {
        lock (syncRoot)
        {
            return state;
        }
    }

    /// <inheritdoc />
    public void SetSuccess(TelemtUsersSyncResult result)
    {
        lock (syncRoot)
        {
            state = new TelemtSyncState(
                true,
                result.SyncedAtUtc,
                state.LastFailedSyncAtUtc,
                state.LastErrorMessage,
                result);
        }
    }

    /// <inheritdoc />
    public void SetFailure(string errorMessage, DateTimeOffset failedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException("Сообщение об ошибке фоновой синхронизации telemt не задано.");
        }

        lock (syncRoot)
        {
            state = new TelemtSyncState(
                state.HasSuccessfulSync,
                state.LastSuccessfulSyncAtUtc,
                failedAtUtc,
                errorMessage.Trim(),
                state.LastResult);
        }
    }
}
