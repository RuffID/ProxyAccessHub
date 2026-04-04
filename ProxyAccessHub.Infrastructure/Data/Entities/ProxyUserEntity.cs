using EFCoreLibrary.Abstractions.Entity;
using ProxyAccessHub.Domain.Enums;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель пользователя для хранения в базе данных.
/// </summary>
public class ProxyUserEntity : IEntity<Guid>
{
    /// <summary>
    /// Локальный идентификатор пользователя.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор пользователя в telemt.
    /// </summary>
    public string TelemtUserId { get; set; } = string.Empty;

    /// <summary>
    /// Полная proxy-ссылка пользователя.
    /// </summary>
    public string ProxyLink { get; set; } = string.Empty;

    /// <summary>
    /// Нормализованный ключ для поиска по proxy-ссылке.
    /// </summary>
    public string ProxyLinkLookupKey { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор сервера пользователя.
    /// </summary>
    public Guid ServerId { get; set; }

    /// <summary>
    /// Код назначенного тарифа.
    /// </summary>
    public Guid TariffId { get; set; }

    /// <summary>
    /// Индивидуальная фиксированная цена периода в рублях.
    /// </summary>
    public decimal? CustomPeriodPriceRub { get; set; }

    /// <summary>
    /// Индивидуальная скидка в процентах.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// Текущий баланс пользователя в рублях.
    /// </summary>
    public decimal BalanceRub { get; set; }

    /// <summary>
    /// Дата оплаченного доступа в UTC.
    /// </summary>
    public DateTimeOffset? AccessPaidToUtc { get; set; }

    /// <summary>
    /// Признак безлимитного доступа пользователя.
    /// </summary>
    public bool IsUnlimited { get; set; }

    /// <summary>
    /// Статус ручной обработки.
    /// </summary>
    public ManualHandlingStatus ManualHandlingStatus { get; set; }
    public string? ManualHandlingReason { get; set; }

    /// <summary>
    /// Служебный тег пользователя в telemt.
    /// </summary>
    public string? UserAdTag { get; set; }

    /// <summary>
    /// Лимит TCP-подключений пользователя.
    /// </summary>
    public int? MaxTcpConnections { get; set; }

    /// <summary>
    /// Лимит трафика пользователя в байтах.
    /// </summary>
    public long? DataQuotaBytes { get; set; }

    /// <summary>
    /// Лимит уникальных IP пользователя.
    /// </summary>
    public int? MaxUniqueIps { get; set; }

    /// <summary>
    /// Текущее количество подключений пользователя.
    /// </summary>
    public int CurrentConnections { get; set; }

    /// <summary>
    /// Текущее количество активных уникальных IP.
    /// </summary>
    public int ActiveUniqueIps { get; set; }

    /// <summary>
    /// Накопленный объём трафика пользователя в октетах.
    /// </summary>
    public long TotalOctets { get; set; }

    /// <summary>
    /// Ревизия telemt на момент последней синхронизации.
    /// </summary>
    public string TelemtRevision { get; set; } = string.Empty;

    /// <summary>
    /// Дата последней синхронизации пользователя в UTC.
    /// </summary>
    public DateTimeOffset LastSyncedAtUtc { get; set; }
}
