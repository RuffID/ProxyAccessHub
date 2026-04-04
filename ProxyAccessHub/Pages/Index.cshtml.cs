using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages;

/// <summary>
/// Стартовая страница приложения.
/// </summary>
public sealed class IndexModel(IOptions<UserAccessOptions> userAccessOptions) : PageModel
{
    /// <summary>
    /// Введённый общий пароль пользователя.
    /// </summary>
    [BindProperty]
    [Display(Name = "Введите пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Сообщение об ошибке входа.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Признак активной пользовательской авторизации.
    /// </summary>
    public bool IsUserAuthenticated =>
        User.HasClaim(UserAccessAuthenticationDefaults.ACCESS_CLAIM_TYPE, UserAccessAuthenticationDefaults.ACCESS_CLAIM_VALUE);

    /// <summary>
    /// Обрабатывает открытие стартовой страницы.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Обрабатывает ввод пользовательского пароля.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(userAccessOptions.Value.SharedPassword))
        {
            throw new InvalidOperationException("В конфигурации не задан общий пользовательский пароль.");
        }

        if (!string.Equals(Password, userAccessOptions.Value.SharedPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Неверный пароль.";
            return Page();
        }

        List<Claim> claims =
        [
            new(UserAccessAuthenticationDefaults.ACCESS_CLAIM_TYPE, UserAccessAuthenticationDefaults.ACCESS_CLAIM_VALUE)
        ];
        ClaimsIdentity identity = new(claims, UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);
        ClaimsPrincipal principal = new(identity);

        await HttpContext.SignInAsync(
            UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true
            });

        return RedirectToPage();
    }

    /// <summary>
    /// Выполняет выход из пользовательской и административной сессии.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);
        await HttpContext.SignOutAsync(AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME);

        return RedirectToPage("/Admin/Login");
    }
}
