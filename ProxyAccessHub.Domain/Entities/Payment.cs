using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Зафиксированный входящий платёж.
/// </summary>
/// <param name="Id">Локальный идентификатор платежа.</param>
/// <param name="PaymentRequestId">Идентификатор заявки на оплату.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="ProviderOperationId">Идентификатор операции у платёжного провайдера.</param>
/// <param name="AmountRub">Сумма, которая была применена к локальной заявке в рублях.</param>
/// <param name="ActualAmountRub">Фактическая сумма входящего платежа в рублях.</param>
/// <param name="ReceivedAtUtc">Дата получения платежа в UTC.</param>
/// <param name="Status">Статус применения платежа.</param>
public record Payment(
    Guid Id,
    Guid PaymentRequestId,
    Guid UserId,
    string ProviderOperationId,
    decimal AmountRub,
    decimal? ActualAmountRub,
    DateTimeOffset ReceivedAtUtc,
    PaymentStatus Status);
