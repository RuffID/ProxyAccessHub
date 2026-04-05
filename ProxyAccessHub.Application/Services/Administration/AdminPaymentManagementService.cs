using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Administration;

/// <summary>
/// Реализует административный просмотр заявок на оплату и сверку с YooMoney.
/// </summary>
public class AdminPaymentManagementService(
    IProxyAccessHubUnitOfWork unitOfWork,
    IYooMoneyWalletClient yooMoneyWalletClient,
    IUserConnectionCreationService userConnectionCreationService) : IAdminPaymentManagementService
{
    private const string STATUS_PENDING = "pending";
    private const string STATUS_PAID = "paid";
    private const string STATUS_OVERPAID = "overpaid";

    /// <inheritdoc />
    public async Task<AdminPaymentsPageData> GetPageDataAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PaymentRequest> paymentRequests = await unitOfWork.PaymentRequests.GetAllAsync(cancellationToken);
        IReadOnlyList<Payment> payments = await unitOfWork.Payments.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        IReadOnlyList<ProxyServer> servers = await unitOfWork.Servers.GetAllAsync(cancellationToken);

        IReadOnlyDictionary<Guid, ProxyUser> usersById = users.ToDictionary(user => user.Id);
        IReadOnlyDictionary<Guid, ProxyServer> serversById = servers.ToDictionary(server => server.Id);
        IReadOnlyDictionary<Guid, Payment[]> paymentsByRequestId = payments
            .GroupBy(payment => payment.PaymentRequestId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(payment => payment.ReceivedAtUtc).ToArray());

        IReadOnlyList<AdminPaymentListItem> items = paymentRequests
            .OrderByDescending(paymentRequest => paymentRequest.CreatedAtUtc)
            .Select(paymentRequest => BuildListItem(
                paymentRequest,
                usersById,
                serversById,
                paymentsByRequestId.GetValueOrDefault(paymentRequest.Id, [])))
            .ToArray();

        IReadOnlyList<AdminServerOption> serverOptions = servers
            .OrderBy(server => server.Name, StringComparer.OrdinalIgnoreCase)
            .Select(server => new AdminServerOption(server.Id, server.Name))
            .ToArray();

        IReadOnlyList<AdminPaymentRequestStatusOption> statuses =
        [
            new(STATUS_PENDING, "Ожидает"),
            new(STATUS_PAID, "Оплачено"),
            new(STATUS_OVERPAID, "Переплата")
        ];

        return new AdminPaymentsPageData(items, serverOptions, statuses);
    }

    /// <inheritdoc />
    public async Task<AdminPaymentCheckDetails> CheckAsync(Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        PaymentRequest paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Заявка на оплату не найдена.");
        _ = await unitOfWork.Users.GetByIdAsync(paymentRequest.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь заявки не найден.");
        IReadOnlyList<Payment> attachedPayments = await unitOfWork.Payments.GetByPaymentRequestIdAsync(paymentRequestId, cancellationToken);

        return BuildCheckDetails(attachedPayments, 0, 0m, 0m, 0, false);
    }

    /// <inheritdoc />
    public async Task<AdminPaymentCheckDetails> ApplyMissingOperationsAsync(Guid paymentRequestId, CancellationToken cancellationToken = default)
    {
        PaymentRequest paymentRequest = await unitOfWork.PaymentRequests.GetByIdAsync(paymentRequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Заявка на оплату не найдена.");
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(paymentRequest.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь заявки не найден.");
        IReadOnlyList<Payment> existingPayments = await unitOfWork.Payments.GetByPaymentRequestIdAsync(paymentRequestId, cancellationToken);

        (DateTimeOffset fromUtc, DateTimeOffset tillUtc) = BuildHistoryRange(paymentRequest);
        IReadOnlyList<YooMoneyOperationHistoryItem> operations = await yooMoneyWalletClient.GetOperationsByLabelAsync(
            paymentRequest.Label,
            fromUtc,
            tillUtc,
            cancellationToken);

        HashSet<string> existingOperationIds = existingPayments
            .Select(payment => payment.ProviderOperationId)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyDictionary<string, YooMoneyOperationHistoryItem> operationsById = operations
            .GroupBy(operation => operation.OperationId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        YooMoneyOperationHistoryItem[] missingOperations = operations
            .Where(operation => IsApplicable(operation, existingOperationIds))
            .OrderBy(operation => operation.OccurredAtUtc)
            .ToArray();
        Payment[] paymentsToRefresh = existingPayments
            .Where(payment => payment.ActualAmountRub is null && operationsById.ContainsKey(payment.ProviderOperationId))
            .ToArray();

        foreach (Payment payment in paymentsToRefresh)
        {
            YooMoneyOperationHistoryItem operation = operationsById[payment.ProviderOperationId];
            await unitOfWork.Payments.UpdateAsync(
                payment with
                {
                    ActualAmountRub = operation.AmountRub
                },
                cancellationToken);
        }

        if (missingOperations.Length > 0)
        {
            if (PendingConnectionUserConventions.IsPending(user))
            {
                await ApplyPendingConnectionOperationsAsync(paymentRequest, user, existingPayments, missingOperations, cancellationToken);
            }
            else
            {
                await ApplyExistingUserOperationsAsync(paymentRequest, user, missingOperations, cancellationToken);
            }
        }
        else if (paymentsToRefresh.Length > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        IReadOnlyList<Payment> updatedPayments = await unitOfWork.Payments.GetByPaymentRequestIdAsync(paymentRequestId, cancellationToken);
        return BuildCheckDetails(
            updatedPayments,
            missingOperations.Length,
            missingOperations.Length * paymentRequest.AmountRub,
            missingOperations.Sum(operation => operation.AmountRub),
            paymentsToRefresh.Length,
            true);
    }

    private static AdminPaymentListItem BuildListItem(
        PaymentRequest paymentRequest,
        IReadOnlyDictionary<Guid, ProxyUser> usersById,
        IReadOnlyDictionary<Guid, ProxyServer> serversById,
        IReadOnlyList<Payment> payments)
    {
        ProxyUser user = usersById.GetValueOrDefault(paymentRequest.UserId)
            ?? throw new KeyNotFoundException($"Пользователь '{paymentRequest.UserId}' для заявки '{paymentRequest.Id}' не найден.");
        ProxyServer server = serversById.GetValueOrDefault(user.ServerId)
            ?? throw new KeyNotFoundException($"Сервер '{user.ServerId}' для пользователя '{user.Id}' не найден.");
        decimal attachedAmountRub = payments.Sum(payment => payment.AmountRub);
        bool isSuccessful = paymentRequest.Status == PaymentRequestStatus.Paid;
        string statusCode = GetStatusCode(paymentRequest, attachedAmountRub);
        string statusName = GetStatusName(statusCode);

        return new AdminPaymentListItem(
            paymentRequest.Id,
            paymentRequest.Label,
            user.Id,
            user.TelemtUserId,
            server.Id,
            server.Name,
            paymentRequest.AmountRub,
            attachedAmountRub,
            payments.Count,
            paymentRequest.CreatedAtUtc,
            paymentRequest.ExpiresAtUtc,
            payments.Count == 0 ? null : payments.Max(payment => payment.ReceivedAtUtc),
            statusCode,
            statusName,
            isSuccessful);
    }

    private static AdminPaymentCheckDetails BuildCheckDetails(
        IReadOnlyList<Payment> attachedPayments,
        int addedPaymentCount,
        decimal addedAppliedAmountRub,
        decimal addedActualAmountRub,
        int updatedActualAmountCount,
        bool hasLoadedYooMoneyOperations)
    {
        IReadOnlyList<AdminPaymentAttachedOperationItem> attachedItems = attachedPayments
            .OrderByDescending(payment => payment.ReceivedAtUtc)
            .Select(payment => new AdminPaymentAttachedOperationItem(
                payment.Id,
                payment.ProviderOperationId,
                payment.AmountRub,
                payment.ActualAmountRub,
                payment.ReceivedAtUtc))
            .ToArray();

        return new AdminPaymentCheckDetails(
            attachedItems,
            addedPaymentCount,
            addedAppliedAmountRub,
            addedActualAmountRub,
            updatedActualAmountCount,
            hasLoadedYooMoneyOperations);
    }

    private async Task ApplyExistingUserOperationsAsync(
        PaymentRequest paymentRequest,
        ProxyUser user,
        IReadOnlyList<YooMoneyOperationHistoryItem> missingOperations,
        CancellationToken cancellationToken)
    {
        foreach (YooMoneyOperationHistoryItem operation in missingOperations)
        {
            await unitOfWork.Payments.AddAsync(
                new Payment(
                    Guid.NewGuid(),
                    paymentRequest.Id,
                    user.Id,
                    operation.OperationId,
                    paymentRequest.AmountRub,
                    operation.AmountRub,
                    operation.OccurredAtUtc,
                    PaymentStatus.Applied),
                cancellationToken);
        }

        ProxyUser updatedUser = user with
        {
            BalanceRub = user.BalanceRub + (missingOperations.Count * paymentRequest.AmountRub),
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null
        };
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.PaymentRequests.UpdateAsync(updatedPaymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPendingConnectionOperationsAsync(
        PaymentRequest paymentRequest,
        ProxyUser pendingUser,
        IReadOnlyList<Payment> existingPayments,
        IReadOnlyList<YooMoneyOperationHistoryItem> missingOperations,
        CancellationToken cancellationToken)
    {
        decimal totalAmountRub = existingPayments.Sum(payment => payment.AmountRub) + (missingOperations.Count * paymentRequest.AmountRub);
        if (totalAmountRub < paymentRequest.AmountRub)
        {
            throw new InvalidOperationException("Суммы найденных операций недостаточно для оплаты нового подключения.");
        }

        DateTimeOffset paidAtUtc = GetConnectionProvisioningDate(existingPayments, missingOperations, paymentRequest.AmountRub);
        NewConnectionProvisioningResult provisioningResult = await userConnectionCreationService.ProvisionPaidConnectionAsync(
            pendingUser,
            paidAtUtc,
            cancellationToken);

        foreach (YooMoneyOperationHistoryItem operation in missingOperations)
        {
            await unitOfWork.Payments.AddAsync(
                new Payment(
                    Guid.NewGuid(),
                    paymentRequest.Id,
                    pendingUser.Id,
                    operation.OperationId,
                    paymentRequest.AmountRub,
                    operation.AmountRub,
                    operation.OccurredAtUtc,
                    PaymentStatus.Applied),
                cancellationToken);
        }

        decimal overpaymentRub = totalAmountRub - paymentRequest.AmountRub;
        ProxyUser updatedUser = provisioningResult.UpdatedUser with
        {
            BalanceRub = provisioningResult.UpdatedUser.BalanceRub + overpaymentRub,
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null
        };
        PaymentRequest updatedPaymentRequest = paymentRequest with
        {
            Status = PaymentRequestStatus.Paid
        };

        await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
        await unitOfWork.Subscriptions.AddAsync(provisioningResult.CreatedSubscription, cancellationToken);
        await unitOfWork.UserTariffAssignments.AddAsync(
            new UserTariffAssignment(
                Guid.NewGuid(),
                updatedUser.Id,
                updatedUser.TariffId,
                paidAtUtc,
                null,
                false,
                null,
                null,
                paidAtUtc,
                "Первичное назначение тарифа после загрузки платежа из YooMoney в админке.",
                "system:yoomoney-admin"),
            cancellationToken);
        await unitOfWork.PaymentRequests.UpdateAsync(updatedPaymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset GetConnectionProvisioningDate(
        IReadOnlyList<Payment> existingPayments,
        IReadOnlyList<YooMoneyOperationHistoryItem> missingOperations,
        decimal requiredAmountRub)
    {
        decimal accumulatedAmountRub = 0m;
        foreach (PaymentCheckpointItem checkpoint in existingPayments
            .Select(payment => new PaymentCheckpointItem(payment.ReceivedAtUtc, payment.AmountRub))
            .Concat(missingOperations.Select(operation => new PaymentCheckpointItem(operation.OccurredAtUtc, requiredAmountRub)))
            .OrderBy(item => item.OccurredAtUtc))
        {
            accumulatedAmountRub += checkpoint.AmountRub;
            if (accumulatedAmountRub >= requiredAmountRub)
            {
                return checkpoint.OccurredAtUtc;
            }
        }

        throw new InvalidOperationException("Не удалось определить момент оплаты нового подключения.");
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset TillUtc) BuildHistoryRange(PaymentRequest paymentRequest)
    {
        DateTimeOffset fromUtc = paymentRequest.CreatedAtUtc.AddDays(-1);
        DateTimeOffset tillUtc = DateTimeOffset.UtcNow.AddDays(1);
        return (fromUtc, tillUtc);
    }

    private static bool IsApplicable(YooMoneyOperationHistoryItem operation, ISet<string> existingOperationIds)
    {
        return !existingOperationIds.Contains(operation.OperationId) && IsSuccessfulIncomingOperation(operation);
    }

    private static bool IsSuccessfulIncomingOperation(YooMoneyOperationHistoryItem operation)
    {
        return string.Equals(operation.Direction, "in", StringComparison.OrdinalIgnoreCase)
            && string.Equals(operation.Status, "success", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStatusCode(PaymentRequest paymentRequest, decimal attachedAmountRub)
    {
        if (attachedAmountRub > paymentRequest.AmountRub)
        {
            return STATUS_OVERPAID;
        }

        return paymentRequest.Status == PaymentRequestStatus.Paid || attachedAmountRub >= paymentRequest.AmountRub
            ? STATUS_PAID
            : STATUS_PENDING;
    }

    private static string GetStatusName(string statusCode)
    {
        return statusCode switch
        {
            STATUS_PENDING => "Ожидает",
            STATUS_PAID => "Оплачено",
            STATUS_OVERPAID => "Переплата",
            _ => throw new InvalidOperationException($"Неподдерживаемый статус заявки '{statusCode}'.")
        };
    }

    private static string GetOperationStatusName(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Неизвестно";
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "success" => "Успешно",
            "refused" => "Отклонено",
            "in_progress" => "В обработке",
            _ => status.Trim()
        };
    }

    private sealed record PaymentCheckpointItem(
        DateTimeOffset OccurredAtUtc,
        decimal AmountRub);
}
