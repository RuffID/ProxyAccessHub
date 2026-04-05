using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Выполняет автопереключение пользователей после завершения trial.
/// </summary>
public class TrialTariffTransitionService(IProxyAccessHubUnitOfWork unitOfWork) : ITrialTariffTransitionService
{
    private const string AUTO_SWITCH_COMMENT = "Автопереключение после завершения trial.";
    private const string AUTO_SWITCH_ASSIGNED_BY = "system:trial-switch";

    /// <inheritdoc />
    public async Task<int> ProcessExpiredTrialsAsync(DateTimeOffset nowUtc, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserTariffAssignment> expiredAssignments = await unitOfWork.UserTariffAssignments
            .GetExpiredTrialAssignmentsAsync(nowUtc, cancellationToken);
        int processedUsers = 0;

        foreach (UserTariffAssignment expiredAssignment in expiredAssignments)
        {
            ProxyUser user = await unitOfWork.Users.GetByIdAsync(expiredAssignment.UserId, cancellationToken)
                ?? throw new KeyNotFoundException($"Пользователь '{expiredAssignment.UserId}' для завершения trial не найден.");
            UserTariffAssignment activeAssignment = await unitOfWork.UserTariffAssignments.GetActiveByUserIdAsync(user.Id, cancellationToken)
                ?? throw new InvalidOperationException($"У пользователя '{user.TelemtUserId}' отсутствует активное назначение тарифа.");

            if (activeAssignment.Id != expiredAssignment.Id)
            {
                continue;
            }

            if (!activeAssignment.IsTrial)
            {
                throw new InvalidOperationException($"Активное назначение пользователя '{user.TelemtUserId}' не является trial.");
            }

            if (user.TariffId != activeAssignment.TariffId)
            {
                throw new InvalidOperationException(
                    $"Текущий тариф пользователя '{user.TelemtUserId}' не совпадает с активным назначением trial.");
            }

            Guid nextTariffId = activeAssignment.NextTariffId
                ?? throw new InvalidOperationException($"У trial-назначения '{activeAssignment.Id}' не указан следующий тариф.");
            TariffDefinition nextTariff = await unitOfWork.Tariffs.GetByIdAsync(nextTariffId, cancellationToken)
                ?? throw new KeyNotFoundException($"Тариф '{nextTariffId}' для автопереключения не найден.");

            if (!nextTariff.IsActive)
            {
                throw new InvalidOperationException($"Тариф '{nextTariff.Name}' для автопереключения неактивен.");
            }

            DateTimeOffset trialEndsAtUtc = GetTrialEndsAtUtc(activeAssignment);
            UserTariffAssignment completedAssignment = activeAssignment with
            {
                EndedAtUtc = trialEndsAtUtc
            };
            UserTariffAssignment nextAssignment = new(
                Guid.NewGuid(),
                user.Id,
                nextTariff.Id,
                trialEndsAtUtc,
                null,
                false,
                null,
                null,
                nowUtc,
                AUTO_SWITCH_COMMENT,
                AUTO_SWITCH_ASSIGNED_BY);
            ProxyUser updatedUser = user with
            {
                TariffId = nextTariff.Id
            };

            await unitOfWork.UserTariffAssignments.UpdateAsync(completedAssignment, cancellationToken);
            await unitOfWork.UserTariffAssignments.AddAsync(nextAssignment, cancellationToken);
            await unitOfWork.Users.UpdateAsync(updatedUser, cancellationToken);
            processedUsers++;
        }

        if (processedUsers > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return processedUsers;
    }

    private static DateTimeOffset GetTrialEndsAtUtc(UserTariffAssignment assignment)
    {
        if (!assignment.IsTrial || !assignment.TrialDurationDays.HasValue || assignment.TrialDurationDays.Value <= 0)
        {
            throw new InvalidOperationException($"Назначение '{assignment.Id}' не содержит корректных данных trial.");
        }

        return assignment.StartedAtUtc.AddDays(assignment.TrialDurationDays.Value);
    }
}
