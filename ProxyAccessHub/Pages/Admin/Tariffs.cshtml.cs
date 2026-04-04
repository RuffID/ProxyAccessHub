using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Models.Administration;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница административного управления тарифами.
/// </summary>
[CookieAuthorize]
[Authorize(AuthenticationSchemes = AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class TariffsModel(IAdminTariffManagementService adminTariffManagementService) : PageModel
{
    /// <summary>
    /// Возвращает данные страницы тарифов.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminTariffsPageData pageData = await adminTariffManagementService.GetPageDataAsync(HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Создаёт новый тариф.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync(string name, string periodPriceRub, string periodMonths, string isActive, string isDefault)
    {
        if (!decimal.TryParse(periodPriceRub, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedPriceRub))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Стоимость тарифа имеет неверный числовой формат.");
        }

        if (!int.TryParse(periodMonths, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPeriodMonths))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Срок тарифа должен быть целым числом месяцев.");
        }

        try
        {
            await adminTariffManagementService.CreateAsync(
                name,
                parsedPriceRub,
                parsedPeriodMonths,
                ParseBool(isActive),
                ParseBool(isDefault),
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Обновляет существующий тариф.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string name, string periodPriceRub, string periodMonths, string isActive, string isDefault)
    {
        if (!decimal.TryParse(periodPriceRub, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedPriceRub))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Стоимость тарифа имеет неверный числовой формат.");
        }

        if (!int.TryParse(periodMonths, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPeriodMonths))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Срок тарифа должен быть целым числом месяцев.");
        }

        try
        {
            await adminTariffManagementService.UpdateAsync(
                id,
                name,
                parsedPriceRub,
                parsedPeriodMonths,
                ParseBool(isActive),
                ParseBool(isDefault),
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    private static bool ParseBool(string value)
    {
        return string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
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
