using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;
using ProxyAccessHub.Domain.ValueObjects;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Обслуживает минимальные административные сценарии управления пользователями.
/// </summary>
public sealed class AdminUserManagementService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffPriceResolver tariffPriceResolver,
    ITelemtSyncStateStore telemtSyncStateStore) : IAdminUserManagementService
{
    private const decimal MAX_CUSTOM_PERIOD_PRICE_RUB = 1000m;

    /// <inheritdoc />
    public async Task<AdminUsersPageData> GetPageDataAsync(bool onlyManualHandling, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, ProxyServer> serversById = servers.ToDictionary(server => server.Id);
        IReadOnlyList<TariffDefinition> tariffs = await unitOfWork.Tariffs.GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, TariffDefinition> tariffsById = tariffs.ToDictionary(tariff => tariff.Id);
        IReadOnlyList<AdminTariffOption> tariffOptions = tariffs
            .Select(tariff => new AdminTariffOption(tariff.Id, tariff.Name, tariff.RequiresRenewal))
            .ToArray();

        IReadOnlyList<AdminUserListItem> userItems = users
            .Where(user => !onlyManualHandling || user.ManualHandlingStatus == ManualHandlingStatus.Required)
            .Select(user => MapUser(user, serversById, tariffsById))
            .ToArray();

        return new AdminUsersPageData(userItems, tariffOptions, BuildTelemtSyncStatus(telemtSyncStateStore.GetState()));
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
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для обновления тарифа не найден.");
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(tariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{tariffId}' не найден.");

        ProxyUser updatedUser = user with
        {
            TariffId = tariff.Id,
            TariffSettings = null
        };

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
        IReadOnlyDictionary<Guid, TariffDefinition> tariffsById)
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

        return new AdminUserListItem(
            user.Id,
            user.TelemtUserId,
            server.Name,
            tariff.Id,
            tariff.Name,
            tariff.RequiresRenewal,
            effectivePeriodPriceRub,
            user.BalanceRub,
            user.AccessPaidToUtc,
            user.ManualHandlingStatus == ManualHandlingStatus.Required,
            GetManualHandlingStatusName(user.ManualHandlingStatus),
            user.ManualHandlingReason,
            user.TariffSettings?.CustomPeriodPriceRub,
            user.TariffSettings?.DiscountPercent);
    }

    private static UserTariffSettings BuildCustomPriceSettings(TariffDefinition tariff, decimal customPeriodPriceRub)
    {
        if (!tariff.RequiresRenewal)
        {
            throw new InvalidOperationException($"Для тарифа '{tariff.Name}' нельзя задавать индивидуальную цену.");
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
}
