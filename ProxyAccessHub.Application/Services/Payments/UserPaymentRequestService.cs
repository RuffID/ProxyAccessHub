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
/// Создаёт локальную заявку на оплату продления и данные формы ЮMoney.
/// </summary>
/// <param name="unitOfWork">UnitOfWork локального хранилища.</param>
/// <param name="tariffCatalog">Каталог тарифов приложения.</param>
/// <param name="tariffPriceResolver">Сервис вычисления эффективной цены периода.</param>
/// <param name="proxyAccessHubOptions">Общие настройки приложения.</param>
/// <param name="yooMoneyOptions">Настройки интеграции с ЮMoney.</param>
public class UserPaymentRequestService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffCatalog tariffCatalog,
    ITariffPriceResolver tariffPriceResolver,
    IOptions<ProxyAccessHubOptions> proxyAccessHubOptions,
    IOptions<YooMoneyOptions> yooMoneyOptions) : IUserPaymentRequestService
{
    private const string YOOMONEY_CONFIRM_URL = "https://yoomoney.ru/quickpay/confirm";

    /// <inheritdoc />
    public async Task<YooMoneyPaymentFormModel> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для оплаты не найден.");

        TariffPlan tariff = tariffCatalog.GetRequired(user.TariffCode);
        if (!tariff.RequiresRenewal || tariff.IsUnlimited || user.IsUnlimited)
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

        if (proxyAccessHubOptions.Value.PaymentRequestLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Время жизни заявки на оплату должно быть больше нуля.");
        }

        if (string.IsNullOrWhiteSpace(yooMoneyOptions.Value.Receiver))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан номер кошелька получателя.");
        }

        if (string.IsNullOrWhiteSpace(yooMoneyOptions.Value.SuccessUrl))
        {
            throw new InvalidOperationException("В конфигурации ЮMoney не задан URL возврата после оплаты.");
        }

        if (!Uri.TryCreate(yooMoneyOptions.Value.SuccessUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("URL возврата после оплаты должен быть абсолютным.");
        }

        DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
        Guid paymentRequestId = Guid.NewGuid();
        string label = paymentRequestId.ToString("N");
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
            yooMoneyOptions.Value.Receiver.Trim(),
            paymentRequest.Label,
            paymentRequest.AmountRub,
            yooMoneyOptions.Value.SuccessUrl.Trim(),
            paymentRequest.ExpiresAtUtc);
    }
}
