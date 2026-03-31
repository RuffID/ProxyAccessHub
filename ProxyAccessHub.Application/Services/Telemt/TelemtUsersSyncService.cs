using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Telemt;

/// <summary>
/// Синхронизирует пользователей telemt в локальное хранилище приложения.
/// </summary>
public class TelemtUsersSyncService : ITelemtUsersSyncService
{
    private const string MISSING_IN_TELEMT_REASON = "Пользователь отсутствует в telemt при фоновой синхронизации.";

    private readonly ITelemtApiClient telemtApiClient;
    private readonly IProxyAccessHubUnitOfWork unitOfWork;
    private readonly ITariffCatalog tariffCatalog;
    private readonly TelemtOptions telemtOptions;
    private readonly ProxyServerPoolOptions proxyServerPoolOptions;

    /// <summary>
    /// Инициализирует сервис синхронизации пользователей telemt.
    /// </summary>
    /// <param name="telemtApiClient">Клиент telemt API.</param>
    /// <param name="unitOfWork">UnitOfWork локального хранилища.</param>
    /// <param name="tariffCatalog">Каталог тарифов приложения.</param>
    /// <param name="telemtOptions">Настройки подключения к telemt.</param>
    /// <param name="proxyServerPoolOptions">Настройки лимитов proxy-серверов.</param>
    public TelemtUsersSyncService(
        ITelemtApiClient telemtApiClient,
        IProxyAccessHubUnitOfWork unitOfWork,
        ITariffCatalog tariffCatalog,
        IOptions<TelemtOptions> telemtOptions,
        IOptions<ProxyServerPoolOptions> proxyServerPoolOptions)
    {
        this.telemtApiClient = telemtApiClient;
        this.unitOfWork = unitOfWork;
        this.tariffCatalog = tariffCatalog;
        this.telemtOptions = telemtOptions.Value;
        this.proxyServerPoolOptions = proxyServerPoolOptions.Value;
    }

    /// <inheritdoc />
    public async Task<TelemtUsersSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        TelemtUsersSnapshot snapshot = await telemtApiClient.GetUsersAsync(cancellationToken);
        DateTimeOffset syncedAtUtc = DateTimeOffset.UtcNow;
        ProxyServer server = await EnsureServerAsync(cancellationToken);
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
            ProxyUser synchronizedUser = BuildUser(server, snapshot.Revision, syncedAtUtc, telemtUser, existingUser);

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
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        ProxyServer? existingServer = servers.FirstOrDefault(server =>
            string.Equals(server.Code, telemtOptions.ServerCode, StringComparison.OrdinalIgnoreCase));

        if (existingServer is not null)
        {
            Uri existingApiUri = CreateApiUri();
            ProxyServer synchronizedServer = existingServer with
            {
                Name = telemtOptions.ServerName.Trim(),
                Host = existingApiUri.Host,
                MaxUsers = proxyServerPoolOptions.DefaultMaxUsersPerServer
            };

            await unitOfWork.Servers.UpdateAsync(synchronizedServer, cancellationToken);
            return synchronizedServer;
        }

        Uri apiUri = CreateApiUri();
        ProxyServer server = new(
            Guid.NewGuid(),
            telemtOptions.ServerCode.Trim(),
            telemtOptions.ServerName.Trim(),
            apiUri.Host,
            proxyServerPoolOptions.DefaultMaxUsersPerServer);

        await unitOfWork.Servers.AddAsync(server, cancellationToken);
        return server;
    }

    private ProxyUser BuildUser(
        ProxyServer server,
        string revision,
        DateTimeOffset syncedAtUtc,
        TelemtUserSnapshot telemtUser,
        ProxyUser? existingUser)
    {
        string primaryProxyLink = PendingConnectionUserConventions.SelectPrimaryProxyLink(telemtUser.Links);
        string proxyLookupKey = PendingConnectionUserConventions.BuildProxyLookupKey(primaryProxyLink);
        string tariffCode = existingUser?.TariffCode ?? tariffCatalog.DefaultTariff.Code;
        bool isUnlimited = existingUser?.IsUnlimited ?? tariffCatalog.GetRequired(tariffCode).IsUnlimited;

        return new ProxyUser(
            existingUser?.Id ?? Guid.NewGuid(),
            telemtUser.Username.Trim(),
            primaryProxyLink,
            proxyLookupKey,
            server.Id,
            tariffCode,
            existingUser?.TariffSettings,
            existingUser?.BalanceRub ?? 0m,
            telemtUser.ExpirationUtc,
            isUnlimited,
            existingUser?.ManualHandlingStatus ?? ManualHandlingStatus.NotRequired,
            existingUser?.ManualHandlingReason,
            telemtUser.UserAdTag,
            telemtUser.MaxTcpConnections,
            telemtUser.DataQuotaBytes,
            telemtUser.MaxUniqueIps,
            telemtUser.CurrentConnections,
            telemtUser.ActiveUniqueIps,
            telemtUser.TotalOctets,
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
        if (!Uri.TryCreate(telemtOptions.ApiBaseUrl, UriKind.Absolute, out Uri? apiUri))
        {
            throw new InvalidOperationException("Адрес telemt API должен быть задан абсолютным URL.");
        }

        return apiUri;
    }

    private void ValidateOptions()
    {
        CreateApiUri();

        if (string.IsNullOrWhiteSpace(telemtOptions.ServerCode))
        {
            throw new InvalidOperationException("Не задан код сервера telemt.");
        }

        if (string.IsNullOrWhiteSpace(telemtOptions.ServerName))
        {
            throw new InvalidOperationException("Не задано название сервера telemt.");
        }

        if (proxyServerPoolOptions.DefaultMaxUsersPerServer <= 0)
        {
            throw new InvalidOperationException("Лимит пользователей на сервере должен быть больше нуля.");
        }
    }
}
