using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Domain.Entities;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Application.Services.Subscriptions;

/// <summary>
/// Реализует бизнес-логику продления подписки на один период в момент очередного списания.
/// </summary>
public class UserSubscriptionRenewalService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffRenewalCalculator tariffRenewalCalculator) : IUserSubscriptionRenewalService
{
    /// <inheritdoc />
    public async Task<UserSubscriptionRenewalResult> TryRenewAsync(
        ProxyUser user,
        Subscription? currentSubscription,
        DateTimeOffset calculatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid billingTariffId = await ResolveBillingTariffIdAsync(user, cancellationToken);
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(billingTariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{user.TariffId}' не найден.");

        TariffRenewalCalculationResult calculation = tariffRenewalCalculator.Calculate(
            new TariffRenewalCalculationRequest(
                tariff,
                user.BalanceRub,
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
            if (!calculation.RenewalApplied)
            {
                return null;
            }

            DateTimeOffset startedAtUtc = calculation.RenewalAppliedFromUtc
                ?? throw new InvalidOperationException("Для созданной подписки должен быть определён момент начала продления.");
            return new Subscription(
                Guid.NewGuid(),
                updatedUser.Id,
                tariff.Id,
                startedAtUtc,
                updatedUser.AccessPaidToUtc,
                tariff.IsUnlimited);
        }

        return currentSubscription with
        {
            TariffId = tariff.Id,
            PaidToUtc = updatedUser.AccessPaidToUtc,
            IsUnlimited = tariff.IsUnlimited
        };
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
