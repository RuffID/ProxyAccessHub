using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Управляет состоянием доступа пользователя через существующую интеграцию с telemt.
/// </summary>
public class ProxyUserAccessService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITelemtApiClient telemtApiClient) : IProxyUserAccessService
{
    /// <inheritdoc />
    public async Task<ProxyUser> ActivateAsync(
        ProxyUser user,
        DateTimeOffset accessPaidToUtc,
        CancellationToken cancellationToken = default)
    {
        if (PendingConnectionUserConventions.IsPending(user))
        {
            throw new InvalidOperationException("Нельзя активировать пользователя, который ещё не создан в telemt.");
        }

        ProxyServer server = await unitOfWork.Servers.GetByIdAsync(user.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException("Сервер пользователя не найден.");

        await telemtApiClient.UpdateUserExpirationAsync(server, user.TelemtUserId, accessPaidToUtc, cancellationToken);

        return user with
        {
            AccessPaidToUtc = accessPaidToUtc,
            IsTelemtAccessActive = true
        };
    }

    /// <inheritdoc />
    public async Task<ProxyUser> DeactivateAsync(
        ProxyUser user,
        CancellationToken cancellationToken = default)
    {
        if (PendingConnectionUserConventions.IsPending(user))
        {
            throw new InvalidOperationException("Нельзя деактивировать пользователя, который ещё не создан в telemt.");
        }

        ProxyServer server = await unitOfWork.Servers.GetByIdAsync(user.ServerId, cancellationToken)
            ?? throw new KeyNotFoundException("Сервер пользователя не найден.");
        DateTimeOffset disabledAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1);

        await telemtApiClient.UpdateUserExpirationAsync(server, user.TelemtUserId, disabledAtUtc, cancellationToken);

        return user with
        {
            IsTelemtAccessActive = false
        };
    }
}
