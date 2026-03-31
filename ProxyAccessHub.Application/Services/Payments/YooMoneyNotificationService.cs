using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Payments;

/// <summary>
/// Обрабатывает уведомления YooMoney и применяет оплату к пользователю.
/// </summary>
public class YooMoneyNotificationService : IYooMoneyNotificationService
{
    private const string EXPECTED_CURRENCY = "643";

    private readonly IProxyAccessHubUnitOfWork unitOfWork;
    private readonly IUserSubscriptionRenewalService userSubscriptionRenewalService;
    private readonly IUserConnectionCreationService userConnectionCreationService;
    private readonly YooMoneyOptions yooMoneyOptions;

    /// <summary>
    /// Инициализирует сервис обработки уведомлений YooMoney.
    /// </summary>
    /// <param name="unitOfWork">UnitOfWork локального хранилища.</param>
    /// <param name="userSubscriptionRenewalService">Сервис применения платежа к пользователю и подписке.</param>
    /// <param name="yooMoneyOptions">Настройки интеграции с ЮMoney.</param>
    public YooMoneyNotificationService(
        IProxyAccessHubUnitOfWork unitOfWork,
        IUserSubscriptionRenewalService userSubscriptionRenewalService,
        IUserConnectionCreationService userConnectionCreationService,
        IOptions<YooMoneyOptions> yooMoneyOptions)
    {
        this.unitOfWork = unitOfWork;
        this.userSubscriptionRenewalService = userSubscriptionRenewalService;
        this.userConnectionCreationService = userConnectionCreationService;
        this.yooMoneyOptions = yooMoneyOptions.Value;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(YooMoneyNotificationModel notification, CancellationToken cancellationToken = default)
    {
        ValidateNotification(notification);
        EnsureSignature(notification);

        Payment? existingPayment = await unitOfWork.Payments.GetByProviderOperationIdAsync(notification.OperationId, cancellationToken);
        if (existingPayment is not null)
        {
            return;
        }

        PaymentRequest paymentRequest = await unitOfWork.PaymentRequests.GetByLabelAsync(notification.Label, cancellationToken)
            ?? throw new KeyNotFoundException("Заявка на оплату по указанному label не найдена.");

        if (paymentRequest.Status != PaymentRequestStatus.Pending)
        {
            throw new InvalidOperationException("Заявка на оплату уже была обработана ранее.");
        }

        if (paymentRequest.AmountRub != notification.Amount)
        {
            throw new InvalidOperationException("Сумма уведомления не совпадает с ожидаемой суммой заявки.");
        }

        ProxyUser user = await unitOfWork.Users.GetByIdAsync(paymentRequest.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для оплаченной заявки не найден.");
        DateTimeOffset receivedAtUtc = ParseNotificationDateTime(notification.DateTimeRaw);

        try
        {
            if (PendingConnectionUserConventions.IsPending(user))
            {
                await ApplyNewConnectionPaymentAsync(paymentRequest, user, notification, receivedAtUtc, cancellationToken);
                return;
            }

            await ApplyRenewalPaymentAsync(paymentRequest, user, notification, receivedAtUtc, cancellationToken);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await PersistManualHandlingAsync(paymentRequest, user, notification, receivedAtUtc, ex.Message, cancellationToken);
            return;
        }
    }

    private async Task PersistSubscriptionAsync(Subscription? currentSubscription, Subscription? updatedSubscription, CancellationToken cancellationToken)
    {
        if (updatedSubscription is null)
        {
            return;
        }

        if (currentSubscription is null)
        {
            await unitOfWork.Subscriptions.AddAsync(updatedSubscription, cancellationToken);
            return;
        }

        await unitOfWork.Subscriptions.UpdateAsync(updatedSubscription, cancellationToken);
    }

    private async Task ApplyRenewalPaymentAsync(
        PaymentRequest paymentRequest,
        ProxyUser user,
        YooMoneyNotificationModel notification,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken)
    {
        Subscription? currentSubscription = await unitOfWork.Subscriptions.GetByUserIdAsync(user.Id, cancellationToken);
        UserSubscriptionRenewalResult renewalResult = userSubscriptionRenewalService.Apply(
            user,
            currentSubscription,
            notification.Amount,
            receivedAtUtc);

        Payment payment = new(
            Guid.NewGuid(),
            paymentRequest.Id,
            user.Id,
            notification.OperationId.Trim(),
            notification.Amount,
            receivedAtUtc,
            PaymentStatus.Applied);
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };

        await unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(renewalResult.UpdatedUser, cancellationToken);
        await PersistSubscriptionAsync(currentSubscription, renewalResult.UpdatedSubscription, cancellationToken);
        await unitOfWork.PaymentRequests.UpdateAsync(updatedPaymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyNewConnectionPaymentAsync(
        PaymentRequest paymentRequest,
        ProxyUser pendingUser,
        YooMoneyNotificationModel notification,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken)
    {
        NewConnectionProvisioningResult provisioningResult = await userConnectionCreationService.ProvisionPaidConnectionAsync(
            pendingUser,
            receivedAtUtc,
            cancellationToken);

        Payment payment = new(
            Guid.NewGuid(),
            paymentRequest.Id,
            pendingUser.Id,
            notification.OperationId.Trim(),
            notification.Amount,
            receivedAtUtc,
            PaymentStatus.Applied);
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };

        await unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(provisioningResult.UpdatedUser, cancellationToken);
        await unitOfWork.Subscriptions.AddAsync(provisioningResult.CreatedSubscription, cancellationToken);
        await unitOfWork.PaymentRequests.UpdateAsync(updatedPaymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistManualHandlingAsync(
        PaymentRequest paymentRequest,
        ProxyUser user,
        YooMoneyNotificationModel notification,
        DateTimeOffset receivedAtUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        Payment payment = new(
            Guid.NewGuid(),
            paymentRequest.Id,
            user.Id,
            notification.OperationId.Trim(),
            notification.Amount,
            receivedAtUtc,
            PaymentStatus.RequiresManualHandling);
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };
        ProxyUser updatedUser = user with
        {
            BalanceRub = user.BalanceRub + notification.Amount,
            ManualHandlingStatus = ManualHandlingStatus.Required,
            ManualHandlingReason = BuildManualHandlingReason(reason)
        };

        await unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.PaymentRequests.UpdateAsync(updatedPaymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildManualHandlingReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Причина ручной обработки платежа не определена.");
        }

        string normalizedReason = reason.Trim();
        return normalizedReason.Length <= 1024
            ? normalizedReason
            : normalizedReason[..1024];
    }

    private void ValidateNotification(YooMoneyNotificationModel notification)
    {
        if (string.IsNullOrWhiteSpace(notification.NotificationType))
        {
            throw new InvalidOperationException("Тип уведомления YooMoney не задан.");
        }

        if (string.IsNullOrWhiteSpace(notification.OperationId))
        {
            throw new InvalidOperationException("Идентификатор операции YooMoney не задан.");
        }

        if (notification.Amount <= 0m)
        {
            throw new InvalidOperationException("Сумма уведомления YooMoney должна быть больше нуля.");
        }

        if (!string.Equals(notification.Currency.Trim(), EXPECTED_CURRENCY, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("YooMoney уведомил о платеже в неподдерживаемой валюте.");
        }

        if (string.IsNullOrWhiteSpace(notification.Label))
        {
            throw new InvalidOperationException("В уведомлении YooMoney отсутствует label локальной заявки.");
        }

        if (string.IsNullOrWhiteSpace(notification.Sha1Hash))
        {
            throw new InvalidOperationException("В уведомлении YooMoney отсутствует подпись sha1_hash.");
        }

        if (string.Equals(notification.Unaccepted.Trim(), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("YooMoney прислал неподтверждённое уведомление о платеже.");
        }
    }

    private void EnsureSignature(YooMoneyNotificationModel notification)
    {
        if (string.IsNullOrWhiteSpace(yooMoneyOptions.NotificationSecret))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан секрет для проверки уведомлений.");
        }

        string signaturePayload = string.Join("&",
            notification.NotificationType.Trim(),
            notification.OperationId.Trim(),
            notification.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            notification.Currency.Trim(),
            notification.DateTimeRaw.Trim(),
            notification.Sender.Trim(),
            notification.CodePro.Trim(),
            yooMoneyOptions.NotificationSecret.Trim(),
            notification.Label.Trim());

        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(signaturePayload));
        string expectedHash = Convert.ToHexString(hash).ToLowerInvariant();
        string actualHash = notification.Sha1Hash.Trim().ToLowerInvariant();

        if (!string.Equals(expectedHash, actualHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Подпись уведомления YooMoney не прошла проверку.");
        }
    }

    private static DateTimeOffset ParseNotificationDateTime(string dateTimeRaw)
    {
        if (!DateTimeOffset.TryParse(dateTimeRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedDateTime))
        {
            throw new InvalidOperationException("Дата операции YooMoney имеет неверный формат.");
        }

        return parsedDateTime;
    }
}
