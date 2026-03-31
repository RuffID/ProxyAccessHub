namespace ProxyAccessHub.Application.Models.Users;

/// <summary>
/// Результат поиска пользователя для продления.
/// </summary>
/// <param name="UserId">Локальный идентификатор пользователя.</param>
/// <param name="TelemtUserId">Идентификатор пользователя в telemt.</param>
/// <param name="ProxyLink">Полная proxy-ссылка пользователя.</param>
/// <param name="TariffCode">Код тарифа пользователя.</param>
/// <param name="TariffName">Название тарифа пользователя.</param>
/// <param name="BalanceRub">Текущий баланс пользователя в рублях.</param>
/// <param name="AccessPaidToUtc">Дата оплаченного доступа в UTC.</param>
/// <param name="IsUnlimited">Признак безлимитного тарифа.</param>
/// <param name="PeriodMonths">Длительность полного периода продления в месяцах.</param>
/// <param name="AmountRequiredRub">Сумма, необходимая для следующего полного периода продления.</param>
/// <param name="SearchKind">Тип совпадения, по которому найден пользователь.</param>
public sealed record UserRenewalLookupResult(
    Guid UserId,
    string TelemtUserId,
    string ProxyLink,
    string TariffCode,
    string TariffName,
    decimal BalanceRub,
    DateTimeOffset? AccessPaidToUtc,
    bool IsUnlimited,
    int PeriodMonths,
    decimal AmountRequiredRub,
    string SearchKind);
