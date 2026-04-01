using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProxyAccessHub.Pages.User;

/// <summary>
/// Страница входа обычного пользователя.
/// </summary>
public sealed class LoginModel : PageModel
{
    /// <summary>
    /// Перенаправляет пользовательский вход на стартовую страницу.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Index");
    }

    /// <summary>
    /// Перенаправляет отправку формы на стартовую страницу.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public IActionResult OnPost()
    {
        return RedirectToPage("/Index");
    }
}
