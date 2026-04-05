namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Координирует доступ к хранилищам MVP-модели приложения.
/// </summary>
public interface IProxyAccessHubUnitOfWork
{
    /// <summary>
    /// Репозиторий пользователей.
    /// </summary>
    IProxyUserRepository Users { get; }

    /// <summary>
    /// Репозиторий серверов.
    /// </summary>
    IProxyServerRepository Servers { get; }

    /// <summary>
    /// Репозиторий тарифов.
    /// </summary>
    ITariffDefinitionRepository Tariffs { get; }

    /// <summary>
    /// Репозиторий заявок на оплату.
    /// </summary>
    IPaymentRequestRepository PaymentRequests { get; }

    /// <summary>
    /// Репозиторий платежей.
    /// </summary>
    IPaymentRepository Payments { get; }

    /// <summary>
    /// Репозиторий подписок.
    /// </summary>
    ISubscriptionRepository Subscriptions { get; }

    /// <summary>
    /// Репозиторий истории назначений тарифов.
    /// </summary>
    IUserTariffAssignmentRepository UserTariffAssignments { get; }

    /// <summary>
    /// Сохраняет накопленные изменения.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача завершения операции.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
