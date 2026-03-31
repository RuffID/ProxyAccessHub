using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.User;

/// <summary>
/// Страница запуска сценария создания нового подключения.
/// </summary>
[Authorize(AuthenticationSchemes = UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class NewConnectionModel : PageModel
{
    private readonly IUserConnectionCreationService userConnectionCreationService;

    /// <summary>
    /// Инициализирует страницу создания нового подключения.
    /// </summary>
    /// <param name="userConnectionCreationService">Сервис пользовательского сценария создания нового подключения.</param>
    public NewConnectionModel(IUserConnectionCreationService userConnectionCreationService)
    {
        this.userConnectionCreationService = userConnectionCreationService;
    }

    /// <summary>
    /// Текущее предложение для нового подключения.
    /// </summary>
    public NewConnectionOffer? Offer { get; private set; }

    /// <summary>
    /// Сформированная HTML-форма оплаты YooMoney.
    /// </summary>
    public YooMoneyPaymentFormModel? PaymentForm { get; private set; }

    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    public async Task OnGetAsync()
    {
        Offer = await userConnectionCreationService.GetOfferAsync(HttpContext.RequestAborted);
    }

    /// <summary>
    /// Создаёт локальную заявку на оплату нового подключения.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostCreatePaymentAsync()
    {
        try
        {
            Offer = await userConnectionCreationService.GetOfferAsync(HttpContext.RequestAborted);
            PaymentForm = await userConnectionCreationService.CreatePaymentAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
