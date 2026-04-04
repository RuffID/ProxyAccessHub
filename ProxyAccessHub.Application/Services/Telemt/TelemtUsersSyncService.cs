using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Telemt;

/// <summary>
/// Синхронизирует пользователей telemt в локальное хранилище приложения.
/// </summary>
public class TelemtUsersSyncService(
    ITelemtApiClient telemtApiClient,
    IProxyAccessHubUnitOfWork unitOfWork,
    IOptions<TelemtOptions> telemtOptions) : ITelemtUsersSyncService
{
    private const string MISSING_IN_TELEMT_REASON = "Пользователь отсутствует в telemt при фоновой синхронизации.";

    /// <inheritdoc />
    public async Task<TelemtUsersSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        TelemtUsersSnapshot snapshot = await telemtApiClient.GetUsersAsync(cancellationToken);
        DateTimeOffset syncedAtUtc = DateTimeOffset.UtcNow;
        ProxyServer server = await EnsureServerAsync(cancellationToken);
        TariffDefinition defaultTariff = await GetDefaultTariffAsync(cancellationToken);
        IReadOnlyList<ProxyUser> localUsers = await unitOfWork.Users.GetAllAsync(cancellationToken);
        Dictionary<string, ProxyUser> localUsersByTelemtId = BuildLocalUsersByTelemtId(localUsers);
        HashSet<string> processedTelemtIds = new(StringComparer.OrdinalIgnoreCase);

        int createdUsers = 0;
        int updatedUsers = 0;
        int markedForManualHandlingUsers = 0;

        foreach (TelemtUserSnapshot telemtUser in snapshot.Users)
        {
            processedTelemtIds.Add(telemtUser.Username.Trim());
            localUsersByTelemtId.TryGetValue(telemtUser.Username.Trim(), out ProxyUser? existingUser);
            ProxyUser synchronizedUser = BuildUser(server, defaultTariff, snapshot.Revision, syncedAtUtc, telemtUser, existingUser);

            if (existingUser is null)
            {
                await unitOfWork.Users.AddAsync(synchronizedUser, cancellationToken);
                createdUsers++;
                continue;
            }

            await unitOfWork.Users.UpdateAsync(synchronizedUser, cancellationToken);
            updatedUsers++;
        }

        foreach (ProxyUser localUser in localUsers)
        {
            if (PendingConnectionUserConventions.IsPending(localUser))
            {
                continue;
            }

            if (processedTelemtIds.Contains(localUser.TelemtUserId))
            {
                continue;
            }

            ProxyUser updatedUser = localUser with
            {
                ManualHandlingStatus = ManualHandlingStatus.Required,
                ManualHandlingReason = MISSING_IN_TELEMT_REASON,
                LastSyncedAtUtc = syncedAtUtc
            };

            await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
            markedForManualHandlingUsers++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TelemtUsersSyncResult(
            server.Code,
            snapshot.Revision,
            createdUsers,
            updatedUsers,
            snapshot.Users.Count,
            markedForManualHandlingUsers,
            syncedAtUtc);
    }

    private async Task<ProxyServer> EnsureServerAsync(CancellationToken cancellationToken)
    {
        Uri apiUri = CreateApiUri();
        ProxyServer server = await unitOfWork.Servers.GetActiveByEndpointAsync(apiUri.Host, apiUri.Port, cancellationToken)
            ?? throw new InvalidOperationException($"Активный сервер с хостом '{apiUri.Host}' и портом '{apiUri.Port}' не найден в базе данных.");

        return server;
    }

    private async Task<TariffDefinition> GetDefaultTariffAsync(CancellationToken cancellationToken)
    {
        TariffDefinition tariff = await unitOfWork.Tariffs.GetDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В базе данных не найден тариф по умолчанию.");

        if (!tariff.IsActive)
        {
            throw new InvalidOperationException($"Тариф по умолчанию '{tariff.Id}' неактивен.");
        }

        return tariff;
    }

    private ProxyUser BuildUser(
        ProxyServer server,
        TariffDefinition defaultTariff,
        string revision,
        DateTimeOffset syncedAtUtc,
        TelemtUserSnapshot telemtUser,
        ProxyUser? existingUser)
    {
        string primaryProxyLink = PendingConnectionUserConventions.SelectPrimaryProxyLink(telemtUser.Links);
        string proxyLookupKey = PendingConnectionUserConventions.BuildProxyLookupKey(primaryProxyLink);
        Guid tariffId = existingUser?.TariffId ?? defaultTariff.Id;

        return new ProxyUser(
            existingUser?.Id ?? Guid.NewGuid(),
            telemtUser.Username.Trim(),
            primaryProxyLink,
            proxyLookupKey,
            server.Id,
            tariffId,
            existingUser?.TariffSettings,
            existingUser?.BalanceRub ?? 0m,
            telemtUser.ExpirationUtc,
            existingUser?.ManualHandlingStatus ?? ManualHandlingStatus.NotRequired,
            existingUser?.ManualHandlingReason,
            telemtUser.UserAdTag,
            telemtUser.MaxTcpConnections,
            telemtUser.MaxUniqueIps,
            revision,
            syncedAtUtc);
    }

    private static Dictionary<string, ProxyUser> BuildLocalUsersByTelemtId(IReadOnlyList<ProxyUser> localUsers)
    {
        Dictionary<string, ProxyUser> usersByTelemtId = new(StringComparer.OrdinalIgnoreCase);

        foreach (ProxyUser localUser in localUsers)
        {
            if (usersByTelemtId.TryAdd(localUser.TelemtUserId, localUser))
            {
                continue;
            }

            throw new InvalidOperationException($"Найдено несколько локальных пользователей с telemt id '{localUser.TelemtUserId}'.");
        }

        return usersByTelemtId;
    }

    private Uri CreateApiUri()
    {
        if (!Uri.TryCreate(telemtOptions.Value.ApiBaseUrl, UriKind.Absolute, out Uri? apiUri))
        {
            throw new InvalidOperationException("Адрес telemt API должен быть задан абсолютным URL.");
        }

        return apiUri;
    }

    private void ValidateOptions()
    {
        CreateApiUri();
    }
}
