using ProxyAccessHub.Domain.Enums;
using ProxyAccessHub.Domain.ValueObjects;

namespace ProxyAccessHub.Domain.Entities;

/// <summary>
/// Локальная модель пользователя для сценариев продления и оплаты.
/// </summary>
/// <param name="Id">Локальный идентификатор пользователя.</param>
/// <param name="TelemtUserId">Идентификатор пользователя в telemt.</param>
/// <param name="ProxyLink">Полная proxy-ссылка пользователя.</param>
/// <param name="ProxyLinkLookupKey">Нормализованный ключ для поиска по proxy-ссылке.</param>
/// <param name="ServerId">Идентификатор сервера пользователя.</param>
/// <param name="TariffId">Идентификатор назначенного тарифа.</param>
/// <param name="TariffSettings">Индивидуальные настройки тарифа пользователя.</param>
/// <param name="BalanceRub">Текущий баланс пользователя в рублях.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="ManualHandlingStatus">Статус ручной обработки.</param>
/// <param name="ManualHandlingReason">Причина перевода в ручную обработку.</param>
/// <param name="UserAdTag">Служебный тег пользователя в telemt.</param>
/// <param name="MaxTcpConnections">Лимит TCP-подключений пользователя.</param>
/// <param name="MaxUniqueIps">Лимит уникальных IP пользователя.</param>
/// <param name="TelemtRevision">Ревизия telemt на момент последней синхронизации.</param>
/// <param name="LastSyncedAtUtc">Дата последней синхронизации пользователя в UTC.</param>
public record ProxyUser(
    Guid Id,
    string TelemtUserId,
    string ProxyLink,
    string ProxyLinkLookupKey,
    Guid ServerId,
    Guid TariffId,
    UserTariffSettings? TariffSettings,
    decimal BalanceRub,
    DateTimeOffset? AccessPaidToUtc,
    bool IsTelemtAccessActive,
    ManualHandlingStatus ManualHandlingStatus,
    string? ManualHandlingReason,
    string? UserAdTag,
    string TelemtRevision,
    DateTimeOffset LastSyncedAtUtc);
