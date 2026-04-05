using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Payments;

/// <summary>
/// Обрабатывает уведомления YooMoney и зачисляет оплату пользователю.
/// </summary>
public class YooMoneyNotificationService(
    IProxyAccessHubUnitOfWork unitOfWork,
    IUserConnectionCreationService userConnectionCreationService,
    IYooMoneySettingsStore yooMoneySettingsStore,
    ILogger<YooMoneyNotificationService> logger) : IYooMoneyNotificationService
{
    private const string EXPECTED_CURRENCY = "643";

    /// <inheritdoc />
    public async Task ProcessAsync(YooMoneyNotificationModel notification, CancellationToken cancellationToken = default)
    {
        ValidateNotification(notification);
        await EnsureSignatureAsync(notification, cancellationToken);

        Payment? existingPayment = await unitOfWork.Payments.GetByProviderOperationIdAsync(notification.OperationId, cancellationToken);
        if (existingPayment is not null)
        {
            logger.LogInformation(
                "Повторное уведомление YooMoney пропущено: OperationId={OperationId}, Label={Label}, Amount={Amount}",
                notification.OperationId,
                notification.Label,
                notification.Amount);
            return;
        }

        PaymentRequest paymentRequest = await unitOfWork.PaymentRequests.GetByLabelAsync(notification.Label, cancellationToken)
            ?? throw new KeyNotFoundException("Заявка на оплату по указанному label не найдена.");


        if (paymentRequest.AmountRub != notification.WithdrawAmount)
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
                logger.LogInformation(
                    "Платёж YooMoney применён к новому подключению: OperationId={OperationId}, UserId={UserId}, Label={Label}, Amount={Amount}",
                    notification.OperationId,
                    user.Id,
                    notification.Label,
                    notification.WithdrawAmount);
                return;
            }

            await ApplyRenewalPaymentAsync(paymentRequest, user, notification, receivedAtUtc, cancellationToken);
            logger.LogInformation(
                "Платёж YooMoney зачислен на баланс пользователя: OperationId={OperationId}, UserId={UserId}, Label={Label}, Amount={Amount}",
                notification.OperationId,
                user.Id,
                notification.Label,
                notification.WithdrawAmount);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await PersistManualHandlingAsync(paymentRequest, user, notification, receivedAtUtc, ex.Message, cancellationToken);
            logger.LogWarning(
                "Платёж YooMoney отправлен на ручную обработку: OperationId={OperationId}, UserId={UserId}, Label={Label}, Amount={Amount}, Reason={Reason}",
                notification.OperationId,
                user.Id,
                notification.Label,
                notification.WithdrawAmount,
                ex.Message);
            return;
        }
    }

    private async Task ApplyRenewalPaymentAsync(
        PaymentRequest paymentRequest,
        ProxyUser user,
        YooMoneyNotificationModel notification,
        DateTimeOffset receivedAtUtc,
        CancellationToken cancellationToken)
    {
        ProxyUser updatedUser = user with
        {
            BalanceRub = user.BalanceRub + notification.WithdrawAmount,
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null
        };

        Payment payment = new(
            Guid.NewGuid(),
            paymentRequest.Id,
            user.Id,
            notification.OperationId.Trim(),
            notification.WithdrawAmount,
            notification.Amount,
            receivedAtUtc,
            PaymentStatus.Applied);
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };

        await unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
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
            notification.WithdrawAmount,
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
        await unitOfWork.UserTariffAssignments.AddAsync(
            new UserTariffAssignment(
                Guid.NewGuid(),
                provisioningResult.UpdatedUser.Id,
                provisioningResult.UpdatedUser.TariffId,
                receivedAtUtc,
                null,
                false,
                null,
                null,
                receivedAtUtc,
                "Первичное назначение тарифа после оплаты нового подключения.",
                "system:yoomoney"),
            cancellationToken);
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
            notification.WithdrawAmount,
            notification.Amount,
            receivedAtUtc,
            PaymentStatus.RequiresManualHandling);
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };
        ProxyUser updatedUser = user with
        {
            BalanceRub = user.BalanceRub + notification.WithdrawAmount,
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

    private async Task EnsureSignatureAsync(YooMoneyNotificationModel notification, CancellationToken cancellationToken)
    {
        YooMoneySettingsSnapshot yooMoneySettings = await yooMoneySettingsStore.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(yooMoneySettings.NotificationSecret))
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
            yooMoneySettings.NotificationSecret.Trim(),
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
