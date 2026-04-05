using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;
using ProxyAccessHub.Domain.Tariffs;
using ProxyAccessHub.Domain.ValueObjects;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Обслуживает административные сценарии управления пользователями.
/// </summary>
public sealed class AdminUserManagementService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffPriceResolver tariffPriceResolver,
    ITelemtSyncStateStore telemtSyncStateStore,
    IProxyUserAccessService proxyUserAccessService,
    IAdminServerManagementService adminServerManagementService,
    IHttpClientFactory httpClientFactory,
    IOptions<ProxyAccessHubOptions> proxyAccessHubOptions) : IAdminUserManagementService
{
    private const decimal MAX_CUSTOM_PERIOD_PRICE_RUB = 1000m;
    private const int API_TIMEOUT_SECONDS = 15;
    private const int UNLIMITED_REACTIVATION_YEARS = 10;
    private static readonly Regex TELEMT_USER_ID_REGEX = new("^[A-Za-z0-9_.-]{1,64}$", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task<AdminUsersPageData> GetPageDataAsync(bool onlyManualHandling, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, ProxyServer> serversById = servers.ToDictionary(server => server.Id);
        IReadOnlyList<TariffDefinition> tariffs = await unitOfWork.Tariffs.GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, TariffDefinition> tariffsById = tariffs.ToDictionary(tariff => tariff.Id);
        IReadOnlyDictionary<Guid, UserTariffAssignment> activeAssignmentsByUserId = (await unitOfWork.UserTariffAssignments.GetActiveAsync(cancellationToken))
            .ToDictionary(assignment => assignment.UserId);
        IReadOnlySet<Guid> userIdsWithTrialHistory = await unitOfWork.UserTariffAssignments.GetUserIdsWithTrialHistoryAsync(cancellationToken);
        IReadOnlyList<AdminTariffOption> tariffOptions = tariffs
            .Select(tariff => new AdminTariffOption(
                tariff.Id,
                tariff.Name,
                tariff.RequiresRenewal,
                tariff.PeriodPriceRub,
                tariff.PeriodMonths))
            .ToArray();
        IReadOnlyList<AdminServerOption> serverOptions = servers
            .Where(server => server.IsActive)
            .OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase)
            .Select(server => new AdminServerOption(server.Id, server.Name))
            .ToArray();

        IReadOnlyList<AdminUserListItem> userItems = users
            .Where(user => !onlyManualHandling || user.ManualHandlingStatus == ManualHandlingStatus.Required)
            .Select(user => MapUser(user, serversById, tariffsById, activeAssignmentsByUserId, userIdsWithTrialHistory))
            .ToArray();

        return new AdminUsersPageData(
            userItems,
            tariffOptions,
            serverOptions,
            BuildTelemtSyncStatus(telemtSyncStateStore.GetState()));
    }

    /// <inheritdoc />
    public async Task UpdateUserTariffPriceAsync(Guid userId, decimal customPeriodPriceRub, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для обновления цены тарифа не найден.");
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(user.TariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{user.TariffId}' не найден.");

        UserTariffSettings tariffSettings = BuildCustomPriceSettings(tariff, customPeriodPriceRub);
        ProxyUser updatedUser = user with
        {
            TariffSettings = tariffSettings
        };

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateUserTariffAsync(Guid userId, Guid tariffId, CancellationToken cancellationToken = default)
    {
        UserTariffAssignment activeAssignment = await unitOfWork.UserTariffAssignments.GetActiveByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("У пользователя отсутствует активное назначение тарифа.");
        DateTimeOffset switchedAtUtc = DateTimeOffset.UtcNow;
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для обновления тарифа не найден.");
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(tariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{tariffId}' не найден.");

        ProxyUser updatedUser = user with
        {
            TariffId = tariff.Id,
            TariffSettings = null
        };

        UserTariffAssignment completedAssignment = activeAssignment with
        {
            EndedAtUtc = switchedAtUtc
        };
        UserTariffAssignment nextAssignment = new(
            Guid.NewGuid(),
            user.Id,
            tariff.Id,
            switchedAtUtc,
            null,
            false,
            null,
            null,
            switchedAtUtc,
            "Ручное изменение тарифа из админки.",
            "admin");

        await unitOfWork.UserTariffAssignments.UpdateAsync(completedAssignment, cancellationToken);
        await unitOfWork.UserTariffAssignments.AddAsync(nextAssignment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AssignTrialAsync(
        Guid userId,
        Guid trialTariffId,
        int trialDurationDays,
        Guid nextTariffId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        if (trialDurationDays <= 0)
        {
            throw new InvalidOperationException("Длительность trial должна быть больше нуля.");
        }

        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для назначения trial не найден.");

        if (PendingConnectionUserConventions.IsPending(user))
        {
            throw new InvalidOperationException("Нельзя назначить trial пользователю, который ещё не создан в telemt.");
        }

        TariffDefinition trialTariff = await unitOfWork.Tariffs.GetByIdAsync(trialTariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{trialTariffId}' не найден.");
        TariffDefinition nextTariff = await unitOfWork.Tariffs.GetByIdAsync(nextTariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{nextTariffId}' не найден.");
        UserTariffAssignment activeAssignment = await unitOfWork.UserTariffAssignments.GetActiveByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("У пользователя отсутствует активное назначение тарифа.");

        if (!trialTariff.IsActive)
        {
            throw new InvalidOperationException($"Тариф '{trialTariff.Name}' для trial неактивен.");
        }

        if (!nextTariff.IsActive)
        {
            throw new InvalidOperationException($"Тариф '{nextTariff.Name}' для автопереключения неактивен.");
        }

        if (activeAssignment.IsTrial)
        {
            throw new InvalidOperationException($"У пользователя '{user.TelemtUserId}' уже есть активный trial.");
        }

        DateTimeOffset assignedAtUtc = DateTimeOffset.UtcNow;
        DateTimeOffset trialEndsAtUtc = assignedAtUtc.AddDays(trialDurationDays);
        DateTimeOffset telemtExpirationUtc = user.AccessPaidToUtc.HasValue && user.AccessPaidToUtc.Value > trialEndsAtUtc
            ? user.AccessPaidToUtc.Value
            : trialEndsAtUtc;
        ProxyUser activatedUser = await proxyUserAccessService.ActivateAsync(
            user,
            telemtExpirationUtc,
            cancellationToken);
        ProxyUser updatedUser = activatedUser with
        {
            TariffId = trialTariff.Id,
            AccessPaidToUtc = telemtExpirationUtc
        };
        UserTariffAssignment completedAssignment = activeAssignment with
        {
            EndedAtUtc = assignedAtUtc
        };
        UserTariffAssignment trialAssignment = new(
            Guid.NewGuid(),
            user.Id,
            trialTariff.Id,
            assignedAtUtc,
            null,
            true,
            trialDurationDays,
            nextTariff.Id,
            assignedAtUtc,
            NormalizeAssignmentComment(comment),
            "admin");

        await unitOfWork.UserTariffAssignments.UpdateAsync(completedAssignment, cancellationToken);
        await unitOfWork.UserTariffAssignments.AddAsync(trialAssignment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreateUserAsync(string telemtUserId, Guid serverId, Guid tariffId, decimal? customPeriodPriceRub, CancellationToken cancellationToken = default)
    {
        string normalizedTelemtUserId = NormalizeTelemtUserId(telemtUserId);
        ProxyUser? existingLocalUser = await unitOfWork.Users.GetByTelemtUserIdAsync(normalizedTelemtUserId, cancellationToken);

        if (existingLocalUser is not null)
        {
            throw new InvalidOperationException($"Пользователь '{normalizedTelemtUserId}' уже существует в локальной базе.");
        }

        ProxyServer server = await unitOfWork.Servers.GetByIdAsync(serverId, cancellationToken)
            ?? throw new KeyNotFoundException("Выбранный сервер не найден.");

        if (!server.IsActive)
        {
            throw new InvalidOperationException($"Сервер '{server.Name}' неактивен.");
        }

        int currentUsersOnServer = (await unitOfWork.Users.GetAllAsync(cancellationToken)).Count(user => user.ServerId == server.Id);

        if (currentUsersOnServer >= server.MaxUsers)
        {
            throw new InvalidOperationException($"Сервер '{server.Name}' достиг лимита пользователей.");
        }

        await adminServerManagementService.CheckConnectionAsync(server.Id, cancellationToken);
        await EnsureTelemtUserDoesNotExistAsync(server, normalizedTelemtUserId, cancellationToken);

        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(tariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{tariffId}' не найден.");
        UserTariffSettings? tariffSettings = customPeriodPriceRub is null
            ? null
            : BuildCustomPriceSettings(tariff, customPeriodPriceRub.Value);

        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        DateTimeOffset? expirationUtc = tariff.RequiresRenewal
            ? TariffPeriodHelper.ApplyPeriods(createdAtUtc, tariff.PeriodMonths)
            : null;
        ValidateTelemtCreationLimits(proxyAccessHubOptions.Value);
        TelemtCreatedUserResult createdUser = await CreateTelemtUserAsync(server, normalizedTelemtUserId, expirationUtc, cancellationToken);
        string proxyLink = PendingConnectionUserConventions.SelectPrimaryProxyLink(createdUser.User.Links);
        string proxyLookupKey = PendingConnectionUserConventions.BuildProxyLookupKey(proxyLink);
        ProxyUser newUser = new(
            Guid.NewGuid(),
            createdUser.User.Username,
            proxyLink,
            proxyLookupKey,
            server.Id,
            tariff.Id,
            tariffSettings,
            0m,
            createdUser.User.ExpirationUtc,
            IsTelemtAccessActive(createdUser.User.ExpirationUtc, createdAtUtc),
            ManualHandlingStatus.NotRequired,
            null,
            createdUser.User.UserAdTag,
            createdUser.Revision,
            createdAtUtc);
        Subscription subscription = new(
            Guid.NewGuid(),
            newUser.Id,
            newUser.TariffId,
            createdAtUtc,
            newUser.AccessPaidToUtc,
            tariff.IsUnlimited);
        UserTariffAssignment initialAssignment = new(
            Guid.NewGuid(),
            newUser.Id,
            tariff.Id,
            createdAtUtc,
            null,
            false,
            null,
            null,
            createdAtUtc,
            "Первичное назначение тарифа при создании пользователя администратором.",
            "admin");

        await unitOfWork.Users.AddAsync(newUser, cancellationToken);
        await unitOfWork.Subscriptions.AddAsync(subscription, cancellationToken);
        await unitOfWork.UserTariffAssignments.AddAsync(initialAssignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для активации не найден.");

        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(user.TariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"РўР°СЂРёС„ '{user.TariffId}' РЅРµ РЅР°Р№РґРµРЅ.");

        if (PendingConnectionUserConventions.IsPending(user))
        {
            throw new InvalidOperationException("Нельзя активировать пользователя, который ещё не создан в telemt.");
        }

        DateTimeOffset activationExpirationUtc = tariff.IsUnlimited
            ? DateTimeOffset.UtcNow.AddYears(UNLIMITED_REACTIVATION_YEARS)
            : DateTimeOffset.MinValue;

        if (!tariff.IsUnlimited && user.AccessPaidToUtc is null)
        {
            throw new InvalidOperationException("Для пользователя без оплаченного срока автоматическая активация недоступна.");
        }

        if (!tariff.IsUnlimited && user.AccessPaidToUtc <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("Нельзя активировать пользователя с истёкшим локальным сроком доступа.");
        }

        if (!tariff.IsUnlimited)
        {
            activationExpirationUtc = user.AccessPaidToUtc!.Value;
        }

        ProxyUser updatedUser = await proxyUserAccessService.ActivateAsync(
            user,
            activationExpirationUtc,
            cancellationToken);

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для деактивации не найден.");

        if (PendingConnectionUserConventions.IsPending(user))
        {
            throw new InvalidOperationException("Нельзя деактивировать пользователя, который ещё не создан в telemt.");
        }

        ProxyUser updatedUser = await proxyUserAccessService.DeactivateAsync(
            user,
            cancellationToken);

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CompleteManualHandlingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для завершения ручной обработки не найден.");

        if (user.ManualHandlingStatus != ManualHandlingStatus.Required)
        {
            throw new InvalidOperationException("У пользователя нет активной ручной обработки для завершения.");
        }

        ProxyUser updatedUser = user with
        {
            ManualHandlingStatus = ManualHandlingStatus.Completed
        };

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private AdminUserListItem MapUser(
        ProxyUser user,
        IReadOnlyDictionary<Guid, ProxyServer> serversById,
        IReadOnlyDictionary<Guid, TariffDefinition> tariffsById,
        IReadOnlyDictionary<Guid, UserTariffAssignment> activeAssignmentsByUserId,
        IReadOnlySet<Guid> userIdsWithTrialHistory)
    {
        if (!serversById.TryGetValue(user.ServerId, out ProxyServer? server))
        {
            throw new KeyNotFoundException($"Для пользователя '{user.TelemtUserId}' не найден сервер '{user.ServerId}'.");
        }

        if (!tariffsById.TryGetValue(user.TariffId, out TariffDefinition? tariff))
        {
            throw new KeyNotFoundException($"Для пользователя '{user.TelemtUserId}' не найден тариф '{user.TariffId}'.");
        }

        decimal effectivePeriodPriceRub = tariffPriceResolver.ResolvePeriodPrice(
            tariff,
            user.TariffSettings is null
                ? null
                : new TariffUserPriceOverride(user.TariffSettings.CustomPeriodPriceRub, user.TariffSettings.DiscountPercent));
        UserTariffAssignment? activeAssignment = activeAssignmentsByUserId.GetValueOrDefault(user.Id);
        string? nextTariffName = null;
        DateTimeOffset? trialEndsAtUtc = null;

        if (activeAssignment?.IsTrial == true)
        {
            Guid nextTariffId = activeAssignment.NextTariffId
                ?? throw new InvalidOperationException("У активного trial не указан следующий тариф.");

            if (!tariffsById.TryGetValue(nextTariffId, out TariffDefinition? nextTariff))
            {
                throw new KeyNotFoundException($"Для пользователя '{user.TelemtUserId}' не найден следующий тариф '{nextTariffId}'.");
            }

            nextTariffName = nextTariff.Name;
            trialEndsAtUtc = GetTrialEndsAtUtc(activeAssignment);
        }

        return new AdminUserListItem(
            user.Id,
            user.TelemtUserId,
            user.ProxyLink,
            server.Name,
            tariff.Id,
            tariff.Name,
            tariff.RequiresRenewal,
            effectivePeriodPriceRub,
            user.BalanceRub,
            user.AccessPaidToUtc,
            user.IsTelemtAccessActive,
            user.ManualHandlingStatus == ManualHandlingStatus.Required,
            GetManualHandlingStatusName(user.ManualHandlingStatus),
            user.ManualHandlingReason,
            user.TariffSettings?.CustomPeriodPriceRub,
            user.TariffSettings?.DiscountPercent,
            userIdsWithTrialHistory.Contains(user.Id),
            activeAssignment?.IsTrial == true,
            activeAssignment?.StartedAtUtc,
            trialEndsAtUtc,
            nextTariffName,
            activeAssignment?.Comment,
            activeAssignment?.AssignedBy);
    }

    private static DateTimeOffset GetTrialEndsAtUtc(UserTariffAssignment assignment)
    {
        if (!assignment.IsTrial || !assignment.TrialDurationDays.HasValue || assignment.TrialDurationDays.Value <= 0)
        {
            throw new InvalidOperationException($"Назначение '{assignment.Id}' не содержит корректных данных trial.");
        }

        return assignment.StartedAtUtc.AddDays(assignment.TrialDurationDays.Value);
    }

    private static string? NormalizeAssignmentComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        string normalizedComment = comment.Trim();
        return normalizedComment.Length <= 1024
            ? normalizedComment
            : normalizedComment[..1024];
    }

    private async Task<TelemtCreatedUserResult> CreateTelemtUserAsync(
        ProxyServer server,
        string telemtUserId,
        DateTimeOffset? expirationUtc,
        CancellationToken cancellationToken)
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);
        Uri requestUri = BuildTelemtUsersUri(server.Host, server.ApiPort);
        using HttpRequestMessage request = new(HttpMethod.Post, requestUri);
        Dictionary<string, object?> requestBody = new()
        {
            ["username"] = telemtUserId,
            ["max_tcp_conns"] = proxyAccessHubOptions.Value.DefaultTelemtMaxTcpConnections,
            ["max_unique_ips"] = proxyAccessHubOptions.Value.DefaultTelemtMaxUniqueIps
        };

        if (expirationUtc is not null)
        {
            requestBody["expiration_rfc3339"] = expirationUtc.Value.UtcDateTime.ToString("O");
        }

        request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException($"Пользователь '{telemtUserId}' уже существует на сервере '{server.Name}'. Ответ telemt: {responseContent}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Не удалось создать пользователя '{telemtUserId}' на сервере '{server.Name}'. Код: {(int)response.StatusCode}. Ответ: {responseContent}");
        }

        using JsonDocument document = JsonDocument.Parse(responseContent);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("ok", out JsonElement okElement) || okElement.ValueKind != JsonValueKind.True)
        {
            throw new InvalidOperationException($"Telemt API вернул неожиданный ответ при создании пользователя '{telemtUserId}'.");
        }

        string revision = root.GetProperty("revision").GetString()
            ?? throw new InvalidOperationException("Telemt API не вернул revision при создании пользователя.");
        JsonElement dataElement = root.GetProperty("data");
        JsonElement userElement = dataElement.GetProperty("user");
        JsonElement linksElement = userElement.GetProperty("links");

        TelemtUserSnapshot userSnapshot = new(
            userElement.GetProperty("username").GetString()
                ?? throw new InvalidOperationException("Telemt API не вернул username созданного пользователя."),
            userElement.TryGetProperty("user_ad_tag", out JsonElement userAdTagElement) && userAdTagElement.ValueKind != JsonValueKind.Null
                ? userAdTagElement.GetString()
                : null,
            userElement.TryGetProperty("max_tcp_conns", out JsonElement maxTcpConnectionsElement) && maxTcpConnectionsElement.ValueKind != JsonValueKind.Null
                ? maxTcpConnectionsElement.GetInt32()
                : null,
            userElement.TryGetProperty("expiration_rfc3339", out JsonElement expirationElement) && expirationElement.ValueKind != JsonValueKind.Null
                ? expirationElement.GetDateTimeOffset()
                : null,
            null,
            userElement.TryGetProperty("max_unique_ips", out JsonElement maxUniqueIpsElement) && maxUniqueIpsElement.ValueKind != JsonValueKind.Null
                ? maxUniqueIpsElement.GetInt32()
                : null,
            0,
            0,
            0,
            new TelemtUserLinks(
                BuildTelemtLinks(linksElement, "classic"),
                BuildTelemtLinks(linksElement, "secure"),
                BuildTelemtLinks(linksElement, "tls")));

        string secret = dataElement.TryGetProperty("secret", out JsonElement secretElement) && secretElement.ValueKind != JsonValueKind.Null
            ? secretElement.GetString() ?? string.Empty
            : string.Empty;

        return new TelemtCreatedUserResult(revision.Trim(), userSnapshot, secret);
    }

    private async Task EnsureTelemtUserDoesNotExistAsync(ProxyServer server, string telemtUserId, CancellationToken cancellationToken)
    {
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);
        Uri requestUri = BuildTelemtUserUri(server.Host, server.ApiPort, telemtUserId);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("Authorization", BuildAuthorizationHeader(server.ApiBearerToken));

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return;
        }

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Пользователь '{telemtUserId}' уже существует на сервере '{server.Name}'.");
        }

        throw new InvalidOperationException($"Не удалось проверить уникальность пользователя '{telemtUserId}' на сервере '{server.Name}'. Код: {(int)response.StatusCode}. Ответ: {responseContent}");
    }

    private static UserTariffSettings BuildCustomPriceSettings(TariffDefinition tariff, decimal customPeriodPriceRub)
    {
        if (!tariff.RequiresRenewal)
        {
            throw new InvalidOperationException($"Для тарифа '{tariff.Name}' нельзя задать индивидуальную цену.");
        }

        if (customPeriodPriceRub <= 0m)
        {
            throw new InvalidOperationException("Индивидуальная цена периода должна быть больше нуля.");
        }

        if (customPeriodPriceRub > MAX_CUSTOM_PERIOD_PRICE_RUB)
        {
            throw new InvalidOperationException($"Индивидуальная цена периода не должна превышать {MAX_CUSTOM_PERIOD_PRICE_RUB:0.##} руб.");
        }

        return new UserTariffSettings(
            decimal.Round(customPeriodPriceRub, 2, MidpointRounding.AwayFromZero),
            null);
    }

    private static bool IsTelemtAccessActive(DateTimeOffset? expirationUtc, DateTimeOffset nowUtc)
    {
        return expirationUtc is null || expirationUtc > nowUtc;
    }

    private static void ValidateTelemtCreationLimits(ProxyAccessHubOptions proxyAccessHubOptions)
    {
        if (proxyAccessHubOptions.DefaultTelemtMaxTcpConnections <= 0)
        {
            throw new InvalidOperationException("В конфигурации ProxyAccessHub должен быть задан положительный лимит TCP-подключений для telemt.");
        }

        if (proxyAccessHubOptions.DefaultTelemtMaxUniqueIps <= 0)
        {
            throw new InvalidOperationException("В конфигурации ProxyAccessHub должен быть задан положительный лимит уникальных IP для telemt.");
        }
    }

    private static IReadOnlyList<string> BuildTelemtLinks(JsonElement linksElement, string propertyName)
    {
        if (!linksElement.TryGetProperty(propertyName, out JsonElement linkElement) || linkElement.ValueKind == JsonValueKind.Null)
        {
            return Array.Empty<string>();
        }

        if (linkElement.ValueKind == JsonValueKind.Array)
        {
            return linkElement.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .Where(link => !string.IsNullOrWhiteSpace(link))
                .Select(link => link!)
                .ToArray();
        }

        if (linkElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Telemt API вернул поле links.{propertyName} в неподдерживаемом формате '{linkElement.ValueKind}'.");
        }

        string? link = linkElement.GetString();
        return string.IsNullOrWhiteSpace(link)
            ? Array.Empty<string>()
            : [link];
    }

    private static string NormalizeTelemtUserId(string telemtUserId)
    {
        if (string.IsNullOrWhiteSpace(telemtUserId))
        {
            throw new InvalidOperationException("Telemt userId не задан.");
        }

        string normalizedTelemtUserId = telemtUserId.Trim();

        if (!TELEMT_USER_ID_REGEX.IsMatch(normalizedTelemtUserId))
        {
            throw new InvalidOperationException("Telemt userId должен содержать только латинские буквы, цифры, '.', '_' или '-' и быть длиной от 1 до 64 символов.");
        }

        return normalizedTelemtUserId;
    }

    private static string GetManualHandlingStatusName(ManualHandlingStatus manualHandlingStatus)
    {
        return manualHandlingStatus switch
        {
            ManualHandlingStatus.NotRequired => "Не требуется",
            ManualHandlingStatus.Required => "Требуется",
            ManualHandlingStatus.Completed => "Завершена",
            _ => throw new InvalidOperationException($"Неизвестный статус ручной обработки '{manualHandlingStatus}'.")
        };
    }

    private static AdminTelemtSyncStatus BuildTelemtSyncStatus(TelemtSyncState syncState)
    {
        if (!syncState.HasSuccessfulSync)
        {
            string pendingMessage = syncState.LastErrorMessage is null
                ? "Успешных проходов фоновой синхронизации ещё не было."
                : $"Последний запуск завершился ошибкой: {syncState.LastErrorMessage}";

            return new AdminTelemtSyncStatus(
                "Ожидает первый успешный проход",
                pendingMessage,
                null,
                syncState.LastFailedSyncAtUtc,
                null,
                null,
                null,
                null,
                null);
        }

        TelemtUsersSyncResult result = syncState.LastResult
            ?? throw new InvalidOperationException("После успешной синхронизации должен быть сохранён её результат.");

        string statusMessage = syncState.LastErrorMessage is null || syncState.LastFailedSyncAtUtc is null
            ? "Последний проход фоновой синхронизации завершился успешно."
            : $"Последняя ошибка была {syncState.LastFailedSyncAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm}: {syncState.LastErrorMessage}";

        return new AdminTelemtSyncStatus(
            "Синхронизация работает",
            statusMessage,
            syncState.LastSuccessfulSyncAtUtc,
            syncState.LastFailedSyncAtUtc,
            result.Revision,
            result.ProcessedUsers,
            result.CreatedUsers,
            result.UpdatedUsers,
            result.MarkedForManualHandlingUsers);
    }

    private static Uri BuildTelemtUsersUri(string host, int apiPort)
    {
        if (!TryExtractApiEndpoint(host, out string? apiScheme, out string? apiHost))
        {
            throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
        }

        if (apiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API должен быть в диапазоне от 1 до 65535.");
        }

        return new UriBuilder(apiScheme, apiHost, apiPort, "/v1/users").Uri;
    }

    private static Uri BuildTelemtUserUri(string host, int apiPort, string telemtUserId)
    {
        if (!TryExtractApiEndpoint(host, out string? apiScheme, out string? apiHost))
        {
            throw new InvalidOperationException($"Хост сервера '{host}' имеет неверный формат.");
        }

        if (apiPort is <= 0 or > 65535)
        {
            throw new InvalidOperationException("Порт API должен быть в диапазоне от 1 до 65535.");
        }

        if (string.IsNullOrWhiteSpace(telemtUserId))
        {
            throw new InvalidOperationException("Telemt userId не задан.");
        }

        return new UriBuilder(apiScheme, apiHost, apiPort, $"/v1/users/{Uri.EscapeDataString(telemtUserId)}").Uri;
    }

    private static string BuildAuthorizationHeader(string apiBearerToken)
    {
        return $"Bearer {NormalizeApiBearerToken(apiBearerToken)}";
    }

    private static string NormalizeApiBearerToken(string apiBearerToken)
    {
        string trimmedApiBearerToken = apiBearerToken.Trim();
        return trimmedApiBearerToken.StartsWith("Bearer ", StringComparison.Ordinal)
            ? trimmedApiBearerToken["Bearer ".Length..].Trim()
            : trimmedApiBearerToken;
    }

    private static bool TryExtractApiEndpoint(string host, out string? apiScheme, out string? apiHost)
    {
        apiScheme = null;
        apiHost = null;

        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        string trimmedHost = host.Trim();

        if (Uri.TryCreate(trimmedHost, UriKind.Absolute, out Uri? absoluteUri))
        {
            if (string.IsNullOrWhiteSpace(absoluteUri.Host))
            {
                return false;
            }

            UriHostNameType absoluteHostNameType = Uri.CheckHostName(absoluteUri.Host);

            if (absoluteHostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
            {
                return false;
            }

            apiScheme = absoluteUri.Scheme;
            apiHost = absoluteUri.Host;
            return true;
        }

        if (IPAddress.TryParse(trimmedHost, out _))
        {
            apiScheme = Uri.UriSchemeHttp;
            apiHost = trimmedHost;
            return true;
        }

        if (Uri.CheckHostName(trimmedHost) is UriHostNameType.Dns)
        {
            apiScheme = Uri.UriSchemeHttp;
            apiHost = trimmedHost;
            return true;
        }

        if (!Uri.TryCreate($"{Uri.UriSchemeHttp}://{trimmedHost}", UriKind.Absolute, out Uri? hostUri))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(hostUri.Host))
        {
            return false;
        }

        UriHostNameType hostNameType = Uri.CheckHostName(hostUri.Host);

        if (hostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6))
        {
            return false;
        }

        apiScheme = Uri.UriSchemeHttp;
        apiHost = hostUri.Host;
        return true;
    }
}
