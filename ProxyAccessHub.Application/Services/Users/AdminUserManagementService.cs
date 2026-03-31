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
public sealed class AdminUserManagementService : IAdminUserManagementService
{
    private readonly IProxyAccessHubUnitOfWork unitOfWork;
    private readonly ITariffCatalog tariffCatalog;
    private readonly ITelemtSyncStateStore telemtSyncStateStore;

    /// <summary>
    /// Инициализирует сервис минимальной админки пользователей.
    /// </summary>
    /// <param name="unitOfWork">UnitOfWork локального хранилища.</param>
    /// <param name="tariffCatalog">Каталог тарифов приложения.</param>
    public AdminUserManagementService(
        IProxyAccessHubUnitOfWork unitOfWork,
        ITariffCatalog tariffCatalog,
        ITelemtSyncStateStore telemtSyncStateStore)
    {
        this.unitOfWork = unitOfWork;
        this.tariffCatalog = tariffCatalog;
        this.telemtSyncStateStore = telemtSyncStateStore;
    }

    /// <inheritdoc />
    public async Task<AdminUsersPageData> GetPageDataAsync(bool onlyManualHandling, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        IReadOnlyDictionary<Guid, ProxyServer> serversById = servers.ToDictionary(server => server.Id);
        IReadOnlyList<AdminTariffOption> tariffs = tariffCatalog.GetAll()
            .Select(tariff => new AdminTariffOption(tariff.Code, tariff.Name, tariff.RequiresRenewal))
            .ToArray();

        IReadOnlyList<AdminUserListItem> userItems = users
            .Where(user => !onlyManualHandling || user.ManualHandlingStatus == ManualHandlingStatus.Required)
            .Select(user => MapUser(user, serversById))
            .ToArray();

        return new AdminUsersPageData(userItems, tariffs, BuildTelemtSyncStatus(telemtSyncStateStore.GetState()));
    }

    /// <inheritdoc />
    public async Task UpdateUserTariffAsync(
        Guid userId,
        string tariffCode,
        decimal? customPeriodPriceRub,
        decimal? discountPercent,
        CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для обновления тарифа не найден.");

        TariffPlan tariff = tariffCatalog.GetRequired(RequireTariffCode(tariffCode));
        UserTariffSettings? tariffSettings = BuildTariffSettings(tariff, customPeriodPriceRub, discountPercent);
        ProxyUser updatedUser = user with
        {
            TariffCode = tariff.Code,
            TariffSettings = tariffSettings
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

    private AdminUserListItem MapUser(ProxyUser user, IReadOnlyDictionary<Guid, ProxyServer> serversById)
    {
        if (!serversById.TryGetValue(user.ServerId, out ProxyServer? server))
        {
            throw new KeyNotFoundException($"Для пользователя '{user.TelemtUserId}' не найден сервер '{user.ServerId}'.");
        }

        TariffPlan tariff = tariffCatalog.GetRequired(user.TariffCode);

        return new AdminUserListItem(
            user.Id,
            user.TelemtUserId,
            server.Name,
            tariff.Code,
            tariff.Name,
            tariff.RequiresRenewal,
            user.BalanceRub,
            user.AccessPaidToUtc,
            user.ManualHandlingStatus == ManualHandlingStatus.Required,
            GetManualHandlingStatusName(user.ManualHandlingStatus),
            user.ManualHandlingReason,
            user.TariffSettings?.CustomPeriodPriceRub,
            user.TariffSettings?.DiscountPercent);
    }

    private static UserTariffSettings? BuildTariffSettings(
        TariffPlan tariff,
        decimal? customPeriodPriceRub,
        decimal? discountPercent)
    {
        if (customPeriodPriceRub is null && discountPercent is null)
        {
            return null;
        }

        if (!tariff.RequiresRenewal)
        {
            throw new InvalidOperationException($"Для тарифа '{tariff.Name}' нельзя задавать индивидуальную цену или скидку.");
        }

        if (customPeriodPriceRub is not null && discountPercent is not null)
        {
            throw new InvalidOperationException("Нельзя одновременно задать индивидуальную цену и скидку.");
        }

        if (customPeriodPriceRub is decimal customPrice)
        {
            if (customPrice <= 0m)
            {
                throw new InvalidOperationException("Индивидуальная цена периода должна быть больше нуля.");
            }

            return new UserTariffSettings(
                decimal.Round(customPrice, 2, MidpointRounding.AwayFromZero),
                null);
        }

        if (discountPercent is decimal discount)
        {
            if (discount <= 0m || discount >= 100m)
            {
                throw new InvalidOperationException("Скидка должна быть больше нуля и меньше ста процентов.");
            }

            return new UserTariffSettings(
                null,
                decimal.Round(discount, 2, MidpointRounding.AwayFromZero));
        }

        throw new InvalidOperationException("Не удалось определить индивидуальные настройки тарифа пользователя.");
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

    private static string RequireTariffCode(string tariffCode)
    {
        if (string.IsNullOrWhiteSpace(tariffCode))
        {
            throw new InvalidOperationException("Не выбран тариф пользователя.");
        }

        return tariffCode.Trim();
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
