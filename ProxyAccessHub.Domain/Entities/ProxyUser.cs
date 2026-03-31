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
/// <param name="TariffCode">Код назначенного тарифа.</param>
/// <param name="TariffSettings">Индивидуальные настройки тарифа пользователя.</param>
/// <param name="BalanceRub">Текущий баланс пользователя в рублях.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="IsUnlimited">Признак безлимитного доступа пользователя.</param>
/// <param name="ManualHandlingStatus">Статус ручной обработки.</param>
/// <param name="ManualHandlingReason">Причина перевода в ручную обработку.</param>
/// <param name="UserAdTag">Служебный тег пользователя в telemt.</param>
/// <param name="MaxTcpConnections">Лимит TCP-подключений пользователя.</param>
/// <param name="DataQuotaBytes">Лимит трафика пользователя в байтах.</param>
/// <param name="MaxUniqueIps">Лимит уникальных IP пользователя.</param>
/// <param name="CurrentConnections">Текущее количество подключений пользователя.</param>
/// <param name="ActiveUniqueIps">Текущее количество активных уникальных IP.</param>
/// <param name="TotalOctets">Накопленный объём трафика пользователя в октетах.</param>
/// <param name="TelemtRevision">Ревизия telemt на момент последней синхронизации.</param>
/// <param name="LastSyncedAtUtc">Дата последней синхронизации пользователя в UTC.</param>
public record ProxyUser(
    Guid Id,
    string TelemtUserId,
    string ProxyLink,
    string ProxyLinkLookupKey,
    Guid ServerId,
    string TariffCode,
    UserTariffSettings? TariffSettings,
    decimal BalanceRub,
    DateTimeOffset? AccessPaidToUtc,
    bool IsUnlimited,
    ManualHandlingStatus ManualHandlingStatus,
    string? ManualHandlingReason,
    string? UserAdTag,
    int? MaxTcpConnections,
    long? DataQuotaBytes,
    int? MaxUniqueIps,
    int CurrentConnections,
    int ActiveUniqueIps,
    long TotalOctets,
    string TelemtRevision,
    DateTimeOffset LastSyncedAtUtc);
