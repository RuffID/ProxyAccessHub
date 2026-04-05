using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Abstractions.Storage;

/// <summary>
/// Репозиторий заявок на оплату.
/// </summary>
public interface IPaymentRequestRepository
{
    /// <summary>
    /// Возвращает заявку по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная заявка или <see langword="null" />.</returns>
    Task<PaymentRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает все заявки на оплату.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список всех заявок.</returns>
    Task<IReadOnlyList<PaymentRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает заявку по внешнему label.
    /// </summary>
    /// <param name="label">Внешний идентификатор заявки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная заявка или <see langword="null" />.</returns>
    Task<PaymentRequest?> GetByLabelAsync(string label, CancellationToken cancellationToken = default);


    /// <summary>
    /// Возвращает последнюю незавершённую заявку пользователя, если она ещё актуальна.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="currentUtc">Текущий момент времени в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Найденная заявка или <see langword="null" />.</returns>
    Task<PaymentRequest?> GetActivePendingByUserIdAsync(
        Guid userId,
        DateTimeOffset currentUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет новую заявку.
    /// </summary>
    /// <param name="paymentRequest">Заявка для добавления.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task AddAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет заявку.
    /// </summary>
    /// <param name="paymentRequest">Актуальное состояние заявки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task UpdateAsync(PaymentRequest paymentRequest, CancellationToken cancellationToken = default);
}
