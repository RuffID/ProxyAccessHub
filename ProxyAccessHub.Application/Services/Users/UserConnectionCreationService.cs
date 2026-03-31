using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Реализует пользовательский сценарий создания нового подключения.
/// </summary>
public sealed class UserConnectionCreationService : IUserConnectionCreationService
{
    private readonly IProxyAccessHubUnitOfWork unitOfWork;
    private readonly ITariffCatalog tariffCatalog;
    private readonly ITariffPriceResolver tariffPriceResolver;
    private readonly IUserPaymentRequestService userPaymentRequestService;
    private readonly ITelemtApiClient telemtApiClient;
    private readonly TelemtOptions telemtOptions;
    private readonly ProxyServerPoolOptions proxyServerPoolOptions;
    private readonly YooMoneyOptions yooMoneyOptions;

    /// <summary>
    /// Инициализирует сервис создания нового подключения.
    /// </summary>
    /// <param name="unitOfWork">UnitOfWork локального хранилища.</param>
    /// <param name="tariffCatalog">Каталог тарифов приложения.</param>
    /// <param name="tariffPriceResolver">Сервис вычисления эффективной стоимости периода.</param>
    /// <param name="userPaymentRequestService">Сервис создания локальных заявок на оплату.</param>
    /// <param name="telemtApiClient">Клиент telemt API.</param>
    /// <param name="telemtOptions">Настройки telemt.</param>
    /// <param name="proxyServerPoolOptions">Настройки лимитов серверов.</param>
    /// <param name="yooMoneyOptions">Настройки YooMoney.</param>
    public UserConnectionCreationService(
        IProxyAccessHubUnitOfWork unitOfWork,
        ITariffCatalog tariffCatalog,
        ITariffPriceResolver tariffPriceResolver,
        IUserPaymentRequestService userPaymentRequestService,
        ITelemtApiClient telemtApiClient,
        IOptions<TelemtOptions> telemtOptions,
        IOptions<ProxyServerPoolOptions> proxyServerPoolOptions,
        IOptions<YooMoneyOptions> yooMoneyOptions)
    {
        this.unitOfWork = unitOfWork;
        this.tariffCatalog = tariffCatalog;
        this.tariffPriceResolver = tariffPriceResolver;
        this.userPaymentRequestService = userPaymentRequestService;
        this.telemtApiClient = telemtApiClient;
        this.telemtOptions = telemtOptions.Value;
        this.proxyServerPoolOptions = proxyServerPoolOptions.Value;
        this.yooMoneyOptions = yooMoneyOptions.Value;
    }

    /// <inheritdoc />
    public async Task<NewConnectionOffer> GetOfferAsync(CancellationToken cancellationToken = default)
    {
        TariffPlan tariff = GetTariffForNewConnection();
        ProxyServer server = await EnsureServerAsync(cancellationToken);
        decimal amountRub = tariffPriceResolver.ResolvePeriodPrice(tariff, null);

        return new NewConnectionOffer(
            tariff.Code,
            tariff.Name,
            tariff.PeriodMonths,
            amountRub,
            server.Name);
    }

    /// <inheritdoc />
    public async Task<YooMoneyPaymentFormModel> CreatePaymentAsync(CancellationToken cancellationToken = default)
    {
        TariffPlan tariff = GetTariffForNewConnection();
        ProxyServer server = await EnsureServerAsync(cancellationToken);
        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        string telemtUserId = PendingConnectionUserConventions.GenerateTelemtUserId();
        ProxyUser pendingUser = new(
            Guid.NewGuid(),
            telemtUserId,
            PendingConnectionUserConventions.BuildPendingProxyLink(telemtUserId),
            PendingConnectionUserConventions.BuildPendingProxyLookupKey(),
            server.Id,
            tariff.Code,
            null,
            0m,
            null,
            tariff.IsUnlimited,
            ManualHandlingStatus.NotRequired,
            null,
            null,
            null,
            null,
            null,
            0,
            0,
            0,
            PendingConnectionUserConventions.GetPendingRevision(),
            createdAtUtc);

        await unitOfWork.Users.AddAsync(pendingUser, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        YooMoneyPaymentFormModel paymentForm = await userPaymentRequestService.CreateAsync(pendingUser.Id, cancellationToken);
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
        TariffPlan tariff = tariffCatalog.GetRequired(user.TariffCode);

        if (user.ManualHandlingStatus == ManualHandlingStatus.Required)
        {
            return new NewConnectionPaymentStatusResult(
                paymentRequest.Id,
                paymentRequest.Label,
                paymentRequest.AmountRub,
                "Требуется ручная обработка",
                "Оплата получена, но автоматическое создание пользователя завершилось ошибкой. Кейc переведён в ручную обработку.",
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

        TariffPlan tariff = GetTariffForNewConnection();
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
            IsUnlimited = tariff.IsUnlimited,
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null,
            UserAdTag = createdUser.User.UserAdTag,
            MaxTcpConnections = createdUser.User.MaxTcpConnections,
            DataQuotaBytes = createdUser.User.DataQuotaBytes,
            MaxUniqueIps = createdUser.User.MaxUniqueIps,
            CurrentConnections = createdUser.User.CurrentConnections,
            ActiveUniqueIps = createdUser.User.ActiveUniqueIps,
            TotalOctets = createdUser.User.TotalOctets,
            TelemtRevision = createdUser.Revision,
            LastSyncedAtUtc = paidAtUtc
        };
        Subscription createdSubscription = new(
            Guid.NewGuid(),
            updatedUser.Id,
            updatedUser.TariffCode,
            paidAtUtc,
            updatedUser.AccessPaidToUtc,
            updatedUser.IsUnlimited);

        return new NewConnectionProvisioningResult(updatedUser, createdSubscription);
    }

    private TariffPlan GetTariffForNewConnection()
    {
        TariffPlan tariff = tariffCatalog.DefaultTariff;

        if (!tariff.RequiresRenewal || tariff.IsUnlimited)
        {
            throw new InvalidOperationException("Для создания нового подключения должен быть настроен оплачиваемый периодический тариф по умолчанию.");
        }

        decimal amountRub = tariffPriceResolver.ResolvePeriodPrice(tariff, null);
        if (amountRub <= 0m)
        {
            throw new InvalidOperationException("Стоимость нового подключения должна быть больше нуля.");
        }

        return tariff;
    }

    private async Task<ProxyServer> EnsureServerAsync(CancellationToken cancellationToken)
    {
        ValidateTelemtOptions();

        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);
        ProxyServer? telemtServer = servers.FirstOrDefault(server =>
            string.Equals(server.Code, telemtOptions.ServerCode.Trim(), StringComparison.OrdinalIgnoreCase));

        if (telemtServer is null)
        {
            Uri apiUri = CreateApiUri();
            telemtServer = new ProxyServer(
                Guid.NewGuid(),
                telemtOptions.ServerCode.Trim(),
                telemtOptions.ServerName.Trim(),
                apiUri.Host,
                proxyServerPoolOptions.DefaultMaxUsersPerServer);

            await unitOfWork.Servers.AddAsync(telemtServer, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            servers = [telemtServer];
        }

        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        Dictionary<Guid, int> userCountsByServerId = users
            .GroupBy(user => user.ServerId)
            .ToDictionary(group => group.Key, group => group.Count());

        ProxyServer? selectedServer = servers
            .OrderBy(server => server.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(server =>
            {
                int userCount = userCountsByServerId.GetValueOrDefault(server.Id);
                return userCount < server.MaxUsers;
            });

        return selectedServer
            ?? throw new InvalidOperationException("Не найден доступный сервер для создания нового подключения.");
    }

    private string BuildPaymentSuccessUrl(Guid paymentRequestId)
    {
        if (string.IsNullOrWhiteSpace(yooMoneyOptions.SuccessUrl))
        {
            throw new InvalidOperationException("В конфигурации YooMoney не задан URL возврата после оплаты.");
        }

        Uri baseUri = new(yooMoneyOptions.SuccessUrl.Trim(), UriKind.Absolute);
        string separator = string.IsNullOrEmpty(baseUri.Query) ? "?" : "&";
        return baseUri + $"{separator}paymentRequestId={paymentRequestId:D}";
    }

    private void ValidateTelemtOptions()
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

    private Uri CreateApiUri()
    {
        if (!Uri.TryCreate(telemtOptions.ApiBaseUrl, UriKind.Absolute, out Uri? apiUri))
        {
            throw new InvalidOperationException("Адрес telemt API должен быть задан абсолютным URL.");
        }

        return apiUri;
    }
}
