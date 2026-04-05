using EFCoreLibrary.Abstractions.Entity;

namespace ProxyAccessHub.Infrastructure.Data.Entities;

/// <summary>
/// Инфраструктурная модель сервера для хранения в базе данных.
/// </summary>
public class ProxyServerEntity : IEntity<Guid>
{
    /// <summary>
    /// Локальный идентификатор сервера.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Код сервера.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Отображаемое название сервера.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Хост или адрес сервера.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Порт telemt API сервера.
    /// </summary>
    public int ApiPort { get; set; }

    /// <summary>
    /// Bearer-токен telemt API сервера.
    /// </summary>
    public string ApiBearerToken { get; set; } = string.Empty;

    /// <summary>
    /// Лимит пользователей на сервере.
    /// </summary>
    public int MaxUsers { get; set; }

    /// <summary>
    /// Признак активности сервера.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Признак включённой фоновой синхронизации сервера.
    /// </summary>
    public bool SyncEnabled { get; set; }

    /// <summary>
    /// Интервал фоновой синхронизации сервера в минутах.
    /// </summary>
    public int SyncIntervalMinutes { get; set; }
}
