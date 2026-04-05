using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Payments;

/// <summary>
/// Создаёт локальную заявку на оплату продления и данные формы YooMoney.
/// </summary>
public class UserPaymentRequestService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffPriceResolver tariffPriceResolver,
    IOptions<ProxyAccessHubOptions> proxyAccessHubOptions,
    IYooMoneySettingsStore yooMoneySettingsStore) : IUserPaymentRequestService
{
    private const string YOOMONEY_CONFIRM_URL = "https://yoomoney.ru/quickpay/confirm";

    /// <inheritdoc />
    public async Task<YooMoneyPaymentFormModel> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        Guid billingTariffId = Guid.Empty;
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для оплаты не найден.");

        billingTariffId = await ResolveBillingTariffIdAsync(user, cancellationToken);

        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(billingTariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{user.TariffId}' не найден.");

        if (!tariff.RequiresRenewal || tariff.IsUnlimited)
        {
            throw new InvalidOperationException("Для безлимитного тарифа онлайн-оплата продления не требуется.");
        }

        decimal amountRub = tariffPriceResolver.ResolvePeriodPrice(
            tariff,
            user.TariffSettings is null
                ? null
                : new TariffUserPriceOverride(user.TariffSettings.CustomPeriodPriceRub, user.TariffSettings.DiscountPercent));

        if (amountRub <= 0m)
        {
            throw new InvalidOperationException("Сумма заявки на оплату должна быть больше нуля.");
        }

        YooMoneySettingsSnapshot yooMoneySettings = await yooMoneySettingsStore.GetAsync(cancellationToken);

        if (proxyAccessHubOptions.Value.PaymentRequestLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Время жизни заявки на оплату должно быть больше нуля.");
        }

        if (string.IsNullOrWhiteSpace(yooMoneySettings.Receiver))
        {
            throw new InvalidOperationException("В конфигурации YooMoney не задан номер кошелька получателя.");
        }

        if (string.IsNullOrWhiteSpace(yooMoneySettings.SuccessUrl))
        {
            throw new InvalidOperationException("В конфигурации YooMoney не задан URL возврата после оплаты.");
        }

        if (!Uri.TryCreate(yooMoneySettings.SuccessUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("URL возврата после оплаты должен быть абсолютным.");
        }

        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        PaymentRequest? activePaymentRequest = await unitOfWork.PaymentRequests.GetActivePendingByUserIdAsync(user.Id, createdAtUtc, cancellationToken);

        if (activePaymentRequest is not null)
        {
            return new YooMoneyPaymentFormModel(
                activePaymentRequest.Id,
                YOOMONEY_CONFIRM_URL,
                yooMoneySettings.Receiver.Trim(),
                activePaymentRequest.Label,
                activePaymentRequest.AmountRub,
                yooMoneySettings.SuccessUrl.Trim(),
                activePaymentRequest.ExpiresAtUtc);
        }

        Guid paymentRequestId = Guid.NewGuid();
        string label = paymentRequestId.ToString("D");
        DateTimeOffset expiresAtUtc = createdAtUtc.AddMinutes(proxyAccessHubOptions.Value.PaymentRequestLifetimeMinutes);

        PaymentRequest paymentRequest = new(
            paymentRequestId,
            user.Id,
            label,
            amountRub,
            createdAtUtc,
            expiresAtUtc,
            PaymentRequestStatus.Pending);

        await unitOfWork.PaymentRequests.AddAsync(paymentRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new YooMoneyPaymentFormModel(
            paymentRequest.Id,
            YOOMONEY_CONFIRM_URL,
            yooMoneySettings.Receiver.Trim(),
            paymentRequest.Label,
            paymentRequest.AmountRub,
            yooMoneySettings.SuccessUrl.Trim(),
            paymentRequest.ExpiresAtUtc);
    }

    private async Task<Guid> ResolveBillingTariffIdAsync(ProxyUser user, CancellationToken cancellationToken)
    {
        UserTariffAssignment? activeAssignment = await unitOfWork.UserTariffAssignments.GetActiveByUserIdAsync(user.Id, cancellationToken);

        if (activeAssignment?.IsTrial != true)
        {
            return user.TariffId;
        }

        return activeAssignment.NextTariffId
            ?? throw new InvalidOperationException($"У активного trial пользователя '{user.TelemtUserId}' не указан следующий тариф.");
    }
}
