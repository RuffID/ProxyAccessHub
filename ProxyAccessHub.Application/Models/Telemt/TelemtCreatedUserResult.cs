namespace ProxyAccessHub.Application.Models.Telemt;

/// <summary>
/// Результат создания пользователя в telemt.
/// </summary>
/// <param name="Revision">Ревизия конфигурации telemt после создания пользователя.</param>
/// <param name="User">Созданный пользователь telemt.</param>
/// <param name="Secret">Итоговый секрет пользователя, возвращённый telemt.</param>
public sealed record TelemtCreatedUserResult(
    string Revision,
    TelemtUserSnapshot User,
    string Secret);
