using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий входящих платежей.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Возвращает платёж по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор платежа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный платёж или <see langword="null" />.</returns>
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает платёж по идентификатору операции провайдера.
    /// </summary>
    /// <param name="providerOperationId">Идентификатор операции провайдера.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденный платёж или <see langword="null" />.</returns>
    Task<Payment?> GetByProviderOperationIdAsync(string providerOperationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все платежи пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список платежей пользователя.</returns>
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет платёж.
    /// </summary>
    /// <param name="payment">Платёж для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
