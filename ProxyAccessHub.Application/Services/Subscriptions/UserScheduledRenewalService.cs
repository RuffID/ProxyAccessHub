using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Subscriptions;
using ProxyAccessHub.Application.Services.Users;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Subscriptions;

/// <summary>
/// Выполняет ежедневное списание стоимости одного периода для пользователей с наступившим сроком оплаты.
/// </summary>
public class UserScheduledRenewalService(
    IProxyAccessHubUnitOfWork unitOfWork,
    IUserSubscriptionRenewalService userSubscriptionRenewalService,
    IProxyUserAccessService proxyUserAccessService) : IUserScheduledRenewalService
{
    /// <inheritdoc />
    public async Task<ScheduledRenewalProcessingResult> ProcessDueRenewalsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);
        ProxyUser[] dueUsers = users
            .Where(user => !PendingConnectionUserConventions.IsPending(user))
            .Where(user => user.ManualHandlingStatus != Domain.Enums.ManualHandlingStatus.Required)
            .Where(user => user.AccessPaidToUtc is null || user.AccessPaidToUtc <= nowUtc)
            .ToArray();

        int renewedUsers = 0;
        int activatedUsers = 0;
        int deactivatedUsers = 0;

        foreach (ProxyUser user in dueUsers)
        {
            Subscription? currentSubscription = await unitOfWork.Subscriptions.GetByUserIdAsync(user.Id, cancellationToken);
            UserSubscriptionRenewalResult renewalResult = await userSubscriptionRenewalService.TryRenewAsync(
                user,
                currentSubscription,
                nowUtc,
                cancellationToken);

            if (!renewalResult.Calculation.RequiresRenewal)
            {
                continue;
            }

            if (renewalResult.Calculation.RenewalApplied)
            {
                DateTimeOffset accessPaidToUtc = renewalResult.UpdatedUser.AccessPaidToUtc
                    ?? throw new InvalidOperationException("После успешного списания должен быть указан новый срок доступа.");
                ProxyUser activatedUser = await proxyUserAccessService.ActivateAsync(
                    renewalResult.UpdatedUser,
                    accessPaidToUtc,
                    cancellationToken);

                await unitOfWork.Users.UpdateAsync(activatedUser, cancellationToken);
                await PersistSubscriptionAsync(currentSubscription, renewalResult.UpdatedSubscription, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                renewedUsers++;

                if (!user.IsTelemtAccessActive)
                {
                    activatedUsers++;
                }

                continue;
            }

            if (!user.IsTelemtAccessActive)
            {
                continue;
            }

            ProxyUser deactivatedUser = await proxyUserAccessService.DeactivateAsync(
                user,
                cancellationToken);
            await unitOfWork.Users.UpdateAsync(deactivatedUser, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            deactivatedUsers++;
        }

        return new ScheduledRenewalProcessingResult(
            dueUsers.Length,
            renewedUsers,
            activatedUsers,
            deactivatedUsers);
    }

    private async Task PersistSubscriptionAsync(
        Subscription? currentSubscription,
        Subscription? updatedSubscription,
        CancellationToken cancellationToken)
    {
        if (updatedSubscription is null)
        {
            throw new InvalidOperationException("После успешного списания должно быть подготовлено состояние подписки.");
        }

        if (currentSubscription is null)
        {
            await unitOfWork.Subscriptions.AddAsync(updatedSubscription, cancellationToken);
            return;
        }

        await unitOfWork.Subscriptions.UpdateAsync(updatedSubscription, cancellationToken);
    }
}
