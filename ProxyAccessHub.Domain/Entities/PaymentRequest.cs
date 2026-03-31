using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Заявка на оплату продления.
/// </summary>
/// <param name="Id">Локальный идентификатор заявки.</param>
/// <param name="UserId">Идентификатор пользователя.</param>
/// <param name="Label">Внешний идентификатор заявки для платёжного провайдера.</param>
/// <param name="AmountRub">Сумма заявки в рублях.</param>
/// <param name="CreatedAtUtc">Дата создания заявки в UTC.</param>
/// <param name="ExpiresAtUtc">Дата истечения заявки в UTC.</param>
/// <param name="Status">Статус заявки.</param>
public record PaymentRequest(
    Guid Id,
    Guid UserId,
    string Label,
    decimal AmountRub,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    PaymentRequestStatus Status);
