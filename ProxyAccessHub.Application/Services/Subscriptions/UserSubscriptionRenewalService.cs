using ProxyAccessHub.Application.Abstractions.Storage;
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
public class UserSubscriptionRenewalService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffRenewalCalculator tariffRenewalCalculator) : IUserSubscriptionRenewalService
{
    /// <inheritdoc />
    public async Task<UserSubscriptionRenewalResult> ApplyAsync(
        ProxyUser user,
        Subscription? currentSubscription,
        decimal paymentAmountRub,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (paymentAmountRub <= 0m)
        {
            throw new InvalidOperationException("Сумма входящего платежа должна быть больше нуля.");
        }

        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(user.TariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{user.TariffId}' не найден.");

        decimal updatedBalanceRub = user.BalanceRub + paymentAmountRub;
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
            ManualHandlingStatus = ManualHandlingStatus.NotRequired,
            ManualHandlingReason = null
        };

        Subscription? updatedSubscription = BuildSubscription(updatedUser, tariff, currentSubscription, calculatedAtUtc, calculation);
        return new UserSubscriptionRenewalResult(updatedUser, updatedSubscription, calculation);
    }

    private static Subscription? BuildSubscription(
        ProxyUser updatedUser,
        TariffDefinition tariff,
        Subscription? currentSubscription,
        DateTimeOffset calculatedAtUtc,
        TariffRenewalCalculationResult calculation)
    {
        if (currentSubscription is null)
        {
            if (!tariff.IsUnlimited && updatedUser.AccessPaidToUtc is null)
            {
                return null;
            }

            DateTimeOffset startedAtUtc = calculation.RenewalAppliedFromUtc ?? calculatedAtUtc;
            return new Subscription(
                Guid.NewGuid(),
                updatedUser.Id,
                updatedUser.TariffId,
                startedAtUtc,
                updatedUser.AccessPaidToUtc,
                tariff.IsUnlimited);
        }

        return currentSubscription with
        {
            TariffId = updatedUser.TariffId,
            PaidToUtc = updatedUser.AccessPaidToUtc,
            IsUnlimited = tariff.IsUnlimited
        };
    }
}
