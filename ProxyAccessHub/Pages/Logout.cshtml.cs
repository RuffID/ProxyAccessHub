using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages;

/// <summary>
/// Страница выхода из пользовательской и административной сессии.
/// </summary>
public sealed class LogoutModel : PageModel
{
    /// <summary>
    /// Выполняет выход из пользовательской и административной сессии по GET-запросу.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);
        await HttpContext.SignOutAsync(AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);

        return RedirectToPage("/Index");
    }
}
