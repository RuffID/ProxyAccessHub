using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Administration;
using ProxyAccessHub.Application.Models.Payments;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница административной настройки ЮMoney.
/// </summary>
[CookieAuthorize]
[Authorize(AuthenticationSchemes = AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)]
public class YooMoneyModel(IAdminYooMoneyManagementService adminYooMoneyManagementService) : PageModel
{
    /// <summary>
    /// Возвращает данные страницы ЮMoney.
    /// </summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        AdminYooMoneyPageData pageData = await adminYooMoneyManagementService.GetPageDataAsync(HttpContext.RequestAborted);
        return new JsonResult(pageData)
        {
            StatusCode = StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Сохраняет настройки ЮMoney.
    /// </summary>
    public async Task<IActionResult> OnPostSaveAsync(
        string receiver,
        string notificationSecret,
        string successUrl,
        string clientId,
        string clientSecret,
        string redirectUri,
        string accessToken,
        string accessTokenExpiresAtUtc)
    {
        DateTimeOffset? parsedExpiresAtUtc = null;
        if (!string.IsNullOrWhiteSpace(accessTokenExpiresAtUtc))
        {
            if (!DateTimeOffset.TryParse(accessTokenExpiresAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset expiresAtUtc))
            {
                return CreateErrorResult(StatusCodes.Status400BadRequest, "Дата окончания действия токена имеет неверный формат.");
            }

            parsedExpiresAtUtc = expiresAtUtc;
        }

        try
        {
            await adminYooMoneyManagementService.SaveSettingsAsync(
                new AdminYooMoneySaveRequest(
                    receiver,
                    notificationSecret,
                    successUrl,
                    clientId,
                    clientSecret,
                    redirectUri,
                    accessToken,
                    parsedExpiresAtUtc),
                HttpContext.RequestAborted);

            return CreateSuccessResult();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return CreateErrorResult(StatusCodes.Status400BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// Запускает OAuth-авторизацию ЮMoney.
    /// </summary>
    public async Task<IActionResult> OnGetConnectAsync()
    {
        try
        {
            YooMoneyAuthorizationRequest request = await adminYooMoneyManagementService.GetAuthorizationRequestAsync(HttpContext.RequestAborted);
            string html = $"""
<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="utf-8">
    <title>Переход в ЮMoney</title>
</head>
<body>
    <form id="yoomoneyConnectForm" method="post" action="https://yoomoney.ru/oauth/authorize">
        <input type="hidden" name="client_id" value="{WebUtility.HtmlEncode(request.ClientId)}">
        <input type="hidden" name="response_type" value="{WebUtility.HtmlEncode(request.ResponseType)}">
        <input type="hidden" name="redirect_uri" value="{WebUtility.HtmlEncode(request.RedirectUri)}">
        <input type="hidden" name="scope" value="{WebUtility.HtmlEncode(request.Scope)}">
    </form>
    <script>document.getElementById('yoomoneyConnectForm').submit();</script>
</body>
</html>
""";

            return Content(html, "text/html", Encoding.UTF8);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return RedirectToPage("/Admin/YooMoney", new
            {
                oauthStatus = "error",
                oauthMessage = ex.Message
            });
        }
    }

    /// <summary>
    /// Принимает callback OAuth-авторизации ЮMoney.
    /// </summary>
    public async Task<IActionResult> OnGetCallbackAsync(string? code, string? error, string? error_description)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            string message = string.IsNullOrWhiteSpace(error_description)
                ? $"ЮMoney вернул ошибку авторизации: {error.Trim()}"
                : $"ЮMoney вернул ошибку авторизации: {error.Trim()} ({error_description.Trim()})";

            return RedirectToPage("/Admin/YooMoney", new
            {
                oauthStatus = "error",
                oauthMessage = message
            });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("/Admin/YooMoney", new
            {
                oauthStatus = "error",
                oauthMessage = "ЮMoney не прислал временный OAuth-код."
            });
        }

        try
        {
            await adminYooMoneyManagementService.ExchangeCodeAsync(code, HttpContext.RequestAborted);
            return RedirectToPage("/Admin/YooMoney", new
            {
                oauthStatus = "success",
                oauthMessage = "OAuth-токен ЮMoney успешно получен и сохранён в конфигурации."
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return RedirectToPage("/Admin/YooMoney", new
            {
                oauthStatus = "error",
                oauthMessage = ex.Message
            });
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
