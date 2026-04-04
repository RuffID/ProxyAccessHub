using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Models.Administration;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница административного управления серверами.
/// </summary>
[CookieAuthorize]
[Authorize(AuthenticationSchemes = AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public sealed class ServersModel(IAdminServerManagementService adminServerManagementService) : PageModel
{
    /// <summary>
    /// Возвращает данные страницы серверов.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminServersPageData pageData = await adminServerManagementService.GetPageDataAsync(HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Создаёт новый сервер.
    /// </summary>
    public async Task<IActionResult> OnPostCreateAsync(string name, string host, string apiPort, string apiBearerToken, string maxUsers, string isActive)
    {
        if (!int.TryParse(apiPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedApiPort))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Порт API должен быть целым числом.");
        }

        if (!int.TryParse(maxUsers, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMaxUsers))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Лимит пользователей должен быть целым числом.");
        }

        try
        {
            await adminServerManagementService.CreateAsync(
                name,
                host,
                parsedApiPort,
                apiBearerToken,
                parsedMaxUsers,
                ParseBool(isActive),
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Обновляет существующий сервер.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateAsync(Guid id, string name, string host, string apiPort, string apiBearerToken, string maxUsers, string isActive)
    {
        if (!int.TryParse(apiPort, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedApiPort))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Порт API должен быть целым числом.");
        }

        if (!int.TryParse(maxUsers, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedMaxUsers))
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, "Лимит пользователей должен быть целым числом.");
        }

        try
        {
            await adminServerManagementService.UpdateAsync(
                id,
                name,
                host,
                parsedApiPort,
                apiBearerToken,
                parsedMaxUsers,
                ParseBool(isActive),
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Проверяет связь с сервером.
    /// </summary>
    /// <param name="id">Идентификатор сервера.</param>
    /// <returns>JSON-результат операции.</returns>
    public async Task<IActionResult> OnPostCheckConnectionAsync(Guid id)
    {
        try
        {
            await adminServerManagementService.CheckConnectionAsync(id, HttpContext.RequestAborted);
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
