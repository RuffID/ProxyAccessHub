using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница просмотра платежей для администратора.
/// </summary>
[CookieAuthorize]
[Authorize(AuthenticationSchemes = AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public class PaymentsModel(IAdminPaymentManagementService adminPaymentManagementService) : PageModel
{
    /// <summary>
    /// Возвращает данные страницы платежей для AJAX-интерфейса.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminPaymentsPageData pageData = await adminPaymentManagementService.GetPageDataAsync(HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Возвращает детали сверки заявки с YooMoney.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор заявки на оплату.</param>
    public async Task<IActionResult> OnGetCheckAsync(Guid paymentRequestId)
    {
        try
        {
            AdminPaymentCheckDetails details = await adminPaymentManagementService.CheckAsync(paymentRequestId, HttpContext.RequestAborted);
            return new JsonResult(details)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Загружает из YooMoney отсутствующие операции заявки и автоматически применяет их к пользователю.
    /// </summary>
    /// <param name="paymentRequestId">Идентификатор заявки на оплату.</param>
    public async Task<IActionResult> OnPostApplyMissingOperationsAsync(Guid paymentRequestId)
    {
        try
        {
            AdminPaymentCheckDetails details = await adminPaymentManagementService.ApplyMissingOperationsAsync(paymentRequestId, HttpContext.RequestAborted);
            return new JsonResult(details)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    private static JsonResult CreateErrorResult(int statusCode, string message)
    {
        return new JsonResult(new
        {
            success = false,
            message
        })
        {
            StatusCode = statusCode
        };
    }
}
