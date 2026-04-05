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
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminUsersPageData pageData = await adminUserManagementService.GetPageDataAsync(false, HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Создаёт пользователя.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync(string telemtUserId, Guid serverId, Guid tariffId, string? customPriceRub)
    {
        decimal? parsedCustomPriceRub = null;

        if (!string.IsNullOrWhiteSpace(customPriceRub))
        {
            if (!decimal.TryParse(customPriceRub, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal customPriceValue))
            {
                return CreateErrorResult(StatusCodes.Status400BadRequest, "Кастомная цена имеет неверный числовой формат.");
            }

            parsedCustomPriceRub = customPriceValue;
        }

        try
        {
            await adminUserManagementService.CreateUserAsync(
                telemtUserId,
                serverId,
                tariffId,
                parsedCustomPriceRub,
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Обновляет индивидуальную цену периода для пользователя.
    /// </summary>
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

    /// <summary>
    /// Назначает пользователю trial-тариф.
    /// </summary>
    public async Task<IActionResult> OnPostAssignTrialAsync(
        Guid userId,
        Guid trialTariffId,
        string trialDurationDays,
        Guid nextTariffId,
        string? comment)
    {
        if (!int.TryParse(trialDurationDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedTrialDurationDays))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Длительность trial должна быть целым числом дней.");
        }

        try
        {
            await adminUserManagementService.AssignTrialAsync(
                userId,
                trialTariffId,
                parsedTrialDurationDays,
                nextTariffId,
                comment,
                HttpContext.RequestAborted);
            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Активирует пользователя в telemt.
    /// </summary>
    public async Task<IActionResult> OnPostActivateAsync(Guid userId)
    {
        try
        {
            await adminUserManagementService.ActivateUserAsync(userId, HttpContext.RequestAborted);
            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Деактивирует пользователя в telemt.
    /// </summary>
    public async Task<IActionResult> OnPostDeactivateAsync(Guid userId)
    {
        try
        {
            await adminUserManagementService.DeactivateUserAsync(userId, HttpContext.RequestAborted);
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
