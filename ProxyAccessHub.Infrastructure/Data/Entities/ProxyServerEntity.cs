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
    /// Лимит пользователей на сервере.
    /// </summary>
    public int MaxUsers { get; set; }
}
