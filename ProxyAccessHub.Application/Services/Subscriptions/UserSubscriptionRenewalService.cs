using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Subscriptions;

/// <summary>
/// Реализует бизнес-логику продления подписки после поступления платежа.
/// </summary>
public class UserSubscriptionRenewalService : IUserSubscriptionRenewalService
{
    private readonly ITariffCatalog tariffCatalog;
    private readonly ITariffRenewalCalculator tariffRenewalCalculator;

    /// <summary>
    /// Инициализирует сервис применения платежа к подписке.
    /// </summary>
    /// <param name="tariffCatalog">Каталог тарифов приложения.</param>
    /// <param name="tariffRenewalCalculator">Сервис расчёта продления.</param>
    public UserSubscriptionRenewalService(
        ITariffCatalog tariffCatalog,
        ITariffRenewalCalculator tariffRenewalCalculator)
    {
        this.tariffCatalog = tariffCatalog;
        this.tariffRenewalCalculator = tariffRenewalCalculator;
    }

    /// <inheritdoc />
    public UserSubscriptionRenewalResult Apply(
        ProxyUser user,
        Subscription? currentSubscription,
        decimal paymentAmountRub,
        DateTimeOffset calculatedAtUtc)
    {
        if (paymentAmountRub <= 0m)
        {
            throw new InvalidOperationException("Сумма входящего платежа должна быть больше нуля.");
        }

        decimal updatedBalanceRub = user.BalanceRub + paymentAmountRub;
        TariffPlan tariff = tariffCatalog.GetRequired(user.TariffCode);
        TariffRenewalCalculationResult calculation = tariffRenewalCalculator.Calculate(
            new TariffRenewalCalculationRequest(
                tariff,
                updatedBalanceRub,
                calculatedAtUtc,
                user.AccessPaidToUtc,
                user.TariffSettings is null
                    ? null
                    : new TariffUserPriceOverride(user.TariffSettings.CustomPeriodPriceRub, user.TariffSettings.DiscountPercent)));

        ProxyUser updatedUser = user with
        {
            BalanceRub = calculation.RemainingBalanceRub,
            AccessPaidToUtc = calculation.AccessPaidToUtc,
            IsUnlimited = tariff.IsUnlimited || user.IsUnlimited,
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null
        };

        Subscription? updatedSubscription = BuildSubscription(updatedUser, currentSubscription, calculatedAtUtc, calculation);
        return new UserSubscriptionRenewalResult(updatedUser, updatedSubscription, calculation);
    }

    private static Subscription? BuildSubscription(
        ProxyUser updatedUser,
        Subscription? currentSubscription,
        DateTimeOffset calculatedAtUtc,
        TariffRenewalCalculationResult calculation)
    {
        if (currentSubscription is null)
        {
            if (!updatedUser.IsUnlimited && updatedUser.AccessPaidToUtc is null)
            {
                return null;
            }

            DateTimeOffset startedAtUtc = calculation.RenewalAppliedFromUtc ?? calculatedAtUtc;
            return new Subscription(
                Guid.NewGuid(),
                updatedUser.Id,
                updatedUser.TariffCode,
                startedAtUtc,
                updatedUser.AccessPaidToUtc,
                updatedUser.IsUnlimited);
        }

        return currentSubscription with
        {
            TariffCode = updatedUser.TariffCode,
            PaidToUtc = updatedUser.AccessPaidToUtc,
            IsUnlimited = updatedUser.IsUnlimited
        };
    }
}
