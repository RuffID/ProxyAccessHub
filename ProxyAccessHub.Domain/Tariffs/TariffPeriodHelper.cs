namespace ProxyAccessHub.Domain.Tariffs;

/// <summary>
/// Вспомогательные правила для стандартных периодов тарифа.
/// </summary>
public static class TariffPeriodHelper
{
    public const int TWO_WEEKS_PERIOD_MONTHS = -14;
    public const int WEEKLY_PERIOD_MONTHS = -7;
    public const int MONTHLY_PERIOD_MONTHS = 1;
    public const int HALF_YEAR_PERIOD_MONTHS = 6;
    public const int YEARLY_PERIOD_MONTHS = 12;
    public const int UNLIMITED_PERIOD_MONTHS = 0;

    /// <summary>
    /// Проверяет, поддерживается ли код периода тарифа.
    /// </summary>
    public static bool IsSupported(int periodMonths)
    {
        return periodMonths == TWO_WEEKS_PERIOD_MONTHS
            || periodMonths == WEEKLY_PERIOD_MONTHS
            || periodMonths == MONTHLY_PERIOD_MONTHS
            || periodMonths == HALF_YEAR_PERIOD_MONTHS
            || periodMonths == YEARLY_PERIOD_MONTHS
            || periodMonths == UNLIMITED_PERIOD_MONTHS;
    }

    /// <summary>
    /// Проверяет, является ли период бессрочным.
    /// </summary>
    public static bool IsUnlimited(int periodMonths)
    {
        return periodMonths == UNLIMITED_PERIOD_MONTHS;
    }

    /// <summary>
    /// Проверяет, требует ли период продления.
    /// </summary>
    public static bool RequiresRenewal(int periodMonths)
    {
        return !IsUnlimited(periodMonths);
    }

    /// <summary>
    /// Вычисляет дату окончания периода тарифа.
    /// </summary>
    public static DateTimeOffset ApplyPeriods(DateTimeOffset startedAtUtc, int periodMonths, int periodsCount = 1)
    {
        if (periodsCount <= 0)
        {
            throw new InvalidOperationException("Количество периодов должно быть больше нуля.");
        }

        return periodMonths switch
        {
            TWO_WEEKS_PERIOD_MONTHS => startedAtUtc.AddDays(14 * periodsCount),
            WEEKLY_PERIOD_MONTHS => startedAtUtc.AddDays(7 * periodsCount),
            MONTHLY_PERIOD_MONTHS => startedAtUtc.AddMonths(periodsCount),
            HALF_YEAR_PERIOD_MONTHS => startedAtUtc.AddMonths(6 * periodsCount),
            YEARLY_PERIOD_MONTHS => startedAtUtc.AddMonths(12 * periodsCount),
            _ when IsUnlimited(periodMonths) => throw new InvalidOperationException("Нельзя вычислить дату окончания для бессрочного тарифа."),
            _ => throw new InvalidOperationException($"Код периода тарифа '{periodMonths}' не поддерживается.")
        };
    }
}
