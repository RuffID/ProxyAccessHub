using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Реализует пользовательский сценарий создания нового подключения.
/// </summary>
public sealed class UserConnectionCreationService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffPriceResolver tariffPriceResolver,
    IUserPaymentRequestService userPaymentRequestService,
    ITelemtApiClient telemtApiClient,
    IOptions<YooMoneyOptions> yooMoneyOptions) : IUserConnectionCreationService
{
    /// <inheritdoc />
    public async Task<NewConnectionOffer> GetOfferAsync(CancellationToken cancellationToken = default)
    {
        TariffDefinition tariff = await GetDefaultTariffForNewConnectionAsync(cancellationToken);
        ProxyServer server = await EnsureServerAsync(cancellationToken);
        decimal amountRub = tariffPriceResolver.ResolvePeriodPrice(tariff, null);

        return new NewConnectionOffer(
            tariff.Id,
            tariff.Name,
            tariff.PeriodMonths,
            amountRub,
            server.Name);
    }

    /// <inheritdoc />
    public async Task<YooMoneyPaymentFormModel> CreatePaymentAsync(CancellationToken cancellationToken = default)
    {
        TariffDefinition tariff = await GetDefaultTariffForNewConnectionAsync(cancellationToken);
        ProxyServer server = await EnsureServerAsync(cancellationToken);
        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        string telemtUserId = PendingConnectionUserConventions.GenerateTelemtUserId();
        ProxyUser pendingUser = new(
            Guid.NewGuid(),
            telemtUserId,
            PendingConnectionUserConventions.BuildPendingProxyLink(telemtUserId),
            PendingConnectionUserConventions.BuildPendingProxyLookupKey(),
            server.Id,
            tariff.Id,
            null,
            0m,
            null,
            ManualHandlingStatus.NotRequired,
            null,
            null,
            null,
            null,
            PendingConnectionUserConventions.GetPendingRevision(),
            createdAtUtc);

        await unitOfWork.Users.AddAsync(pendingUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        YooMoneyPaymentFormModel paymentForm = await userPaymentRequestService.GetOrCreateAsync(pendingUser.Id, cancellationToken);
        return paymentForm with
        {
            SuccessUrl = BuildPaymentSuccessUrl(paymentForm.PaymentRequestId)
        };
    }

    /// <inheritdoc />
    public async Task<NewConnectionPaymentStatusResult> GetPaymentStatusAsync(Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        PaymentRequest paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Заявка на оплату нового подключения не найдена.");
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(paymentRequest.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Локальный пользователь заявки на оплату не найден.");
        TariffDefinition tariff = await GetRequiredTariffAsync(user.TariffId, cancellationToken);

        if (user.ManualHandlingStatus == ManualHandlingStatus.Required)
        {
            return new NewConnectionPaymentStatusResult(
                paymentRequest.Id,
                paymentRequest.Label,
                paymentRequest.AmountRub,
                "Требуется ручная обработка",
                "Оплата получена, но автоматическое создание пользователя завершилось ошибкой. Кейс переведён в ручную обработку.",
                false,
                true,
                paymentRequest.ExpiresAtUtc,
                null,
                null,
                tariff.Name,
                null,
                user.ManualHandlingReason);
        }

        if (paymentRequest.Status == PaymentRequestStatus.Pending)
        {
            return new NewConnectionPaymentStatusResult(
                paymentRequest.Id,
                paymentRequest.Label,
                paymentRequest.AmountRub,
                "Ожидает оплату",
                "Платёж ещё не подтверждён YooMoney. После оплаты вернитесь на эту страницу или обновите её.",
                false,
                false,
                paymentRequest.ExpiresAtUtc,
                null,
                null,
                tariff.Name,
                null,
                null);
        }

        if (PendingConnectionUserConventions.IsPending(user))
        {
            return new NewConnectionPaymentStatusResult(
                paymentRequest.Id,
                paymentRequest.Label,
                paymentRequest.AmountRub,
                "Оплата получена",
                "Платёж подтверждён. Система ожидает завершения автоматического создания пользователя.",
                false,
                false,
                paymentRequest.ExpiresAtUtc,
                null,
                null,
                tariff.Name,
                null,
                null);
        }

        return new NewConnectionPaymentStatusResult(
            paymentRequest.Id,
            paymentRequest.Label,
            paymentRequest.AmountRub,
            "Подключение создано",
            "Пользователь успешно создан, подключение готово к использованию.",
            true,
            false,
            paymentRequest.ExpiresAtUtc,
            user.TelemtUserId,
            user.ProxyLink,
            tariff.Name,
            user.AccessPaidToUtc,
            null);
    }

    /// <inheritdoc />
    public async Task<NewConnectionProvisioningResult> ProvisionPaidConnectionAsync(
        ProxyUser pendingUser,
        DateTimeOffset paidAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (!PendingConnectionUserConventions.IsPending(pendingUser))
        {
            throw new InvalidOperationException("Локальный пользователь не находится в состоянии ожидания создания нового подключения.");
        }

        TariffDefinition tariff = await GetDefaultTariffForNewConnectionAsync(cancellationToken);
        DateTimeOffset expirationUtc = paidAtUtc.AddMonths(tariff.PeriodMonths);
        TelemtCreatedUserResult createdUser = await telemtApiClient.CreateUserAsync(
            pendingUser.TelemtUserId,
            expirationUtc,
            cancellationToken);
        string proxyLink = PendingConnectionUserConventions.SelectPrimaryProxyLink(createdUser.User.Links);
        string proxyLookupKey = PendingConnectionUserConventions.BuildProxyLookupKey(proxyLink);
        ProxyUser updatedUser = pendingUser with
        {
            TelemtUserId = createdUser.User.Username,
            ProxyLink = proxyLink,
            ProxyLinkLookupKey = proxyLookupKey,
            AccessPaidToUtc = createdUser.User.ExpirationUtc,
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null,
            UserAdTag = createdUser.User.UserAdTag,
            MaxTcpConnections = createdUser.User.MaxTcpConnections,
            MaxUniqueIps = createdUser.User.MaxUniqueIps,
            TelemtRevision = createdUser.Revision,
            LastSyncedAtUtc = paidAtUtc
        };
        Subscription createdSubscription = new(
            Guid.NewGuid(),
            updatedUser.Id,
            updatedUser.TariffId,
            paidAtUtc,
            updatedUser.AccessPaidToUtc,
            tariff.IsUnlimited);

        return new NewConnectionProvisioningResult(updatedUser, createdSubscription);
    }

    private async Task<TariffDefinition> GetDefaultTariffForNewConnectionAsync(CancellationToken cancellationToken)
    {
        TariffDefinition tariff = await unitOfWork.Tariffs.GetDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("В базе данных не найден тариф по умолчанию.");

        if (!tariff.IsActive)
        {
            throw new InvalidOperationException($"Тариф по умолчанию '{tariff.Id}' неактивен.");
        }

        if (!tariff.RequiresRenewal || tariff.IsUnlimited)
        {
            throw new InvalidOperationException("Для создания нового подключения должен быть настроен активный периодический тариф по умолчанию.");
        }

        decimal amountRub = tariffPriceResolver.ResolvePeriodPrice(tariff, null);
        if (amountRub <= 0m)
        {
            throw new InvalidOperationException("Стоимость нового подключения должна быть больше нуля.");
        }

        return tariff;
    }

    private async Task<TariffDefinition> GetRequiredTariffAsync(Guid tariffId, CancellationToken cancellationToken)
    {
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(tariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{tariffId}' не найден.");
        return tariff;
    }

    private async Task<ProxyServer> EnsureServerAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyServer> activeServers = servers
            .Where(server => server.IsActive)
            .ToArray();

        if (activeServers.Count == 0)
        {
            throw new InvalidOperationException("В базе данных нет активных серверов для создания нового подключения.");
        }

        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        Dictionary<Guid, int> userCountsByServerId = users
            .GroupBy(user => user.ServerId)
            .ToDictionary(group => group.Key, group => group.Count());

        ProxyServer? selectedServer = activeServers
            .OrderBy(server => userCountsByServerId.GetValueOrDefault(server.Id))
            .ThenBy(server => server.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(server => userCountsByServerId.GetValueOrDefault(server.Id) < server.MaxUsers);

        return selectedServer
            ?? throw new InvalidOperationException("Не найден доступный активный сервер для создания нового подключения.");
    }

    private string BuildPaymentSuccessUrl(Guid paymentRequestId)
    {
        if (string.IsNullOrWhiteSpace(yooMoneyOptions.Value.SuccessUrl))
        {
            throw new InvalidOperationException("В конфигурации YooMoney не задан URL возврата после оплаты.");
        }

        Uri baseUri = new(yooMoneyOptions.Value.SuccessUrl.Trim(), UriKind.Absolute);
        string separator = string.IsNullOrEmpty(baseUri.Query) ? "?" : "&";
        return baseUri + $"{separator}paymentRequestId={paymentRequestId:D}";
    }
}
