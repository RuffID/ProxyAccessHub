using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Configuration;

namespace ProxyAccessHub.Pages;

/// <summary>
/// Стартовая страница пополнения баланса.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly YooMoneyOptions yooMoneyOptions;

    /// <summary>
    /// Инициализирует модель стартовой страницы.
    /// </summary>
    /// <param name="yooMoneyOptions">Настройки интеграции с ЮMoney.</param>
    public IndexModel(IOptions<YooMoneyOptions> yooMoneyOptions)
    {
        this.yooMoneyOptions = yooMoneyOptions.Value;
    }

    /// <summary>
    /// Адрес виджета ЮMoney для пополнения баланса.
    /// </summary>
    public string WidgetUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    public void OnGet()
    {
        WidgetUrl = $"https://yoomoney.ru/quickpay/fundraise/button?billNumber={yooMoneyOptions.BillNumber}&";
    }
}
