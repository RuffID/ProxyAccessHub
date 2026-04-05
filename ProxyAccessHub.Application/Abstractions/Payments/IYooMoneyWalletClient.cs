using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Application.Abstractions.Payments;

/// <summary>
/// Предоставляет доступ к wallet API YooMoney для сверки операций.
/// </summary>
public interface IYooMoneyWalletClient
{
    /// <summary>
    /// Возвращает входящие операции YooMoney по метке заявки.
    /// </summary>
    /// <param name="label">Метка заявки.</param>
    /// <param name="fromUtc">Начало периода поиска в UTC.</param>
    /// <param name="tillUtc">Конец периода поиска в UTC.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Список операций YooMoney.</returns>
    Task<IReadOnlyList<YooMoneyOperationHistoryItem>> GetOperationsByLabelAsync(
        string label,
        DateTimeOffset fromUtc,
        DateTimeOffset tillUtc,
        CancellationToken cancellationToken = default);
}
