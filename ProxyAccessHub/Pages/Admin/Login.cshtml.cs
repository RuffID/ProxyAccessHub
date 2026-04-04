using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.Admin;

/// <summary>
/// Страница входа администратора.
/// </summary>
[CookieAuthorize]
public sealed class LoginModel : PageModel
{
    private readonly AdminAccessOptions adminAccessOptions;

    /// <summary>
    /// Инициализирует страницу входа администратора.
    /// </summary>
    /// <param name="adminAccessOptions">Настройки доступа администратора.</param>
    public LoginModel(IOptions<AdminAccessOptions> adminAccessOptions)
    {
        this.adminAccessOptions = adminAccessOptions.Value;
    }

    /// <summary>
    /// Введённый пароль администратора.
    /// </summary>
    [BindProperty]
    [Display(Name = "Пароль администратора")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Сообщение об ошибке входа.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public IActionResult OnGet()
    {
        if (User.HasClaim(AdminAccessAuthenticationDefaults.ACCESS_CLAIM_TYPE, AdminAccessAuthenticationDefaults.ACCESS_CLAIM_VALUE))
        {
            return RedirectToPage("/Admin/Users");
        }

        return Page();
    }

    /// <summary>
    /// Обрабатывает отправку формы входа.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(adminAccessOptions.Password))
        {
            throw new InvalidOperationException("В конфигурации не задан пароль администратора.");
        }

        if (!string.Equals(Password, adminAccessOptions.Password, StringComparison.Ordinal))
        {
            ErrorMessage = "Неверный пароль администратора.";
            return Page();
        }

        List<Claim> claims =
        [
            new(AdminAccessAuthenticationDefaults.ACCESS_CLAIM_TYPE, AdminAccessAuthenticationDefaults.ACCESS_CLAIM_VALUE)
        ];
        ClaimsIdentity identity = new(claims, AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(
            AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true
            });

        return RedirectToPage("/Admin/Users");
    }
}
