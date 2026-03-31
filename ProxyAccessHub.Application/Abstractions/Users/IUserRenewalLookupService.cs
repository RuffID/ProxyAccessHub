using ProxyAccessHub.Application.Models.Users;

namespace ProxyAccessHub.Application.Abstractions.Users;

/// <summary>
/// Ищет пользователя для сценария продления и подготавливает данные для экрана.
/// </summary>
public interface IUserRenewalLookupService
{
    /// <summary>
    /// Ищет пользователя по telemt id или окончанию proxy-ссылки.
    /// </summary>
    /// <param name="searchValue">Поисковое значение, введённое пользователем.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные найденного пользователя и расчёт продления.</returns>
    Task<UserRenewalLookupResult> FindAsync(string searchValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Возвращает данные пользователя для экрана продления по локальному идентификатору.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные пользователя и расчёт продления.</returns>
    Task<UserRenewalLookupResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
