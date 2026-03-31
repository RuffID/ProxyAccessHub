using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.User;

/// <summary>
/// Страница результата оплаты нового подключения.
/// </summary>
[Authorize(AuthenticationSchemes = UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class NewConnectionResultModel : PageModel
{
    private readonly IUserConnectionCreationService userConnectionCreationService;

    /// <summary>
    /// Инициализирует страницу результата оплаты нового подключения.
    /// </summary>
    /// <param name="userConnectionCreationService">Сервис пользовательского сценария создания нового подключения.</param>
    public NewConnectionResultModel(IUserConnectionCreationService userConnectionCreationService)
    {
        this.userConnectionCreationService = userConnectionCreationService;
    }

    /// <summary>
    /// Текущее состояние заявки на оплату.
    /// </summary>
    public NewConnectionPaymentStatusResult? PaymentStatus { get; private set; }

    /// <summary>
    /// Сообщение об ошибке.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор локальной заявки на оплату.</param>
    public async Task OnGetAsync(Guid? paymentRequestId)
    {
        if (paymentRequestId is null || paymentRequestId == Guid.Empty)
        {
            ErrorMessage = "Не передан идентификатор заявки на оплату нового подключения.";
            return;
        }

        try
        {
            PaymentStatus = await userConnectionCreationService.GetPaymentStatusAsync(paymentRequestId.Value, HttpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }
    }
}
