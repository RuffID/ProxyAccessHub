using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница списка пользователей для администратора.
/// </summary>
[CookieAuthorize]
[Authorize(AuthenticationSchemes = AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class UsersModel(IAdminUserManagementService adminUserManagementService) : PageModel
{
    /// <summary>
    /// Возвращает данные страницы пользователей для AJAX-интерфейса.
    /// </summary>
    /// <returns>JSON с данными пользователей и статусом синхронизации.</returns>
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminUsersPageData pageData = await adminUserManagementService.GetPageDataAsync(false, HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Обновляет индивидуальную цену периода для пользователя.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="priceRub">Новое значение цены периода в рублях.</param>
    /// <returns>JSON-результат операции.</returns>
    public async Task<IActionResult> OnPostUpdateTariffPriceAsync(Guid userId, string priceRub)
    {
        if (!decimal.TryParse(priceRub, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedPriceRub))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Цена тарифа имеет неверный числовой формат.");
        }

        try
        {
            await adminUserManagementService.UpdateUserTariffPriceAsync(userId, parsedPriceRub, HttpContext.RequestAborted);
            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Обновляет назначенный пользователю тариф.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="tariffId">Идентификатор нового тарифа.</param>
    /// <returns>JSON-результат операции.</returns>
    public async Task<IActionResult> OnPostUpdateTariffAsync(Guid userId, Guid tariffId)
    {
        try
        {
            await adminUserManagementService.UpdateUserTariffAsync(userId, tariffId, HttpContext.RequestAborted);
            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    private static JsonResult CreateSuccessResult()
    {
        return new JsonResult(new
        {
            success = true
        })
        {
            StatusCode = StatusCodes.Status200OK
        };
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
