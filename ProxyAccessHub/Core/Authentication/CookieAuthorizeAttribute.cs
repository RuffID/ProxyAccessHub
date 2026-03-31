using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProxyAccessHub.Core.Authentication;

/// <summary>
/// Проверяет наличие cookie-аутентификации для Razor Pages и выполняет перенаправление.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CookieAuthorizeAttribute(string cookieName = AdminAccessAuthenticationDefaults.COOKIE_NAME) : Attribute, IAsyncPageFilter, IOrderedFilter
{
    /// <summary>
    /// Порядок выполнения фильтра страницы.
    /// </summary>
    public int Order { get; set; }

    /// <inheritdoc />
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (context.HandlerInstance is not PageModel)
        {
            throw new InvalidOperationException($"{nameof(CookieAuthorizeAttribute)} can only be used on Razor Page models.");
        }

        HttpContext httpContext = context.HttpContext;
        HttpRequest request = httpContext.Request;
        string path = request.Path.ToString();
        bool isLoginPage = string.Equals(path, AdminAccessAuthenticationDefaults.LOGIN_PATH, StringComparison.OrdinalIgnoreCase);
        bool hasCookie = request.Cookies.TryGetValue(cookieName, out string? cookieValue) && !string.IsNullOrWhiteSpace(cookieValue);
        bool isAuthenticated = httpContext.User.Identity?.IsAuthenticated == true;

        if (!hasCookie || !isAuthenticated)
        {
            if (!isLoginPage)
            {
                httpContext.Response.Redirect(AdminAccessAuthenticationDefaults.LOGIN_PATH);
                return;
            }

            await next();
            return;
        }

        if (isLoginPage)
        {
            httpContext.Response.Redirect(AdminAccessAuthenticationDefaults.DEFAULT_SUCCESS_PATH);
            return;
        }

        await next();
    }

    /// <inheritdoc />
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }
}
