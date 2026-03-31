using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.User;

/// <summary>
/// Страница продления существующей подписки.
/// </summary>
[Authorize(AuthenticationSchemes = UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class RenewModel : PageModel
{
    private readonly IUserPaymentRequestService userPaymentRequestService;
    private readonly IUserRenewalLookupService userRenewalLookupService;

    /// <summary>
    /// Инициализирует страницу продления существующей подписки.
    /// </summary>
    /// <param name="userRenewalLookupService">Сервис поиска пользователя для продления.</param>
    public RenewModel(
        IUserPaymentRequestService userPaymentRequestService,
        IUserRenewalLookupService userRenewalLookupService)
    {
        this.userPaymentRequestService = userPaymentRequestService;
        this.userRenewalLookupService = userRenewalLookupService;
    }

    /// <summary>
    /// Значение для поиска пользователя.
    /// </summary>
    [BindProperty]
    [Display(Name = "Telemt id или окончание proxy-ссылки")]
    public string SearchValue { get; set; } = string.Empty;

    /// <summary>
    /// Сообщение об ошибке поиска.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Найденный пользователь и рассчитанные данные продления.
    /// </summary>
    public UserRenewalLookupResult? LookupResult { get; private set; }

    /// <summary>
    /// Сформированная HTML-форма оплаты ЮMoney.
    /// </summary>
    public YooMoneyPaymentFormModel? PaymentForm { get; private set; }

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Обрабатывает отправку формы поиска.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            LookupResult = await userRenewalLookupService.FindAsync(SearchValue, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }

    /// <summary>
    /// Создаёт локальную заявку на оплату и форму ЮMoney.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostCreatePaymentAsync(Guid userId)
    {
        try
        {
            LookupResult = await userRenewalLookupService.GetByUserIdAsync(userId, HttpContext.RequestAborted);
            SearchValue = LookupResult.TelemtUserId;
            PaymentForm = await userPaymentRequestService.CreateAsync(userId, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
