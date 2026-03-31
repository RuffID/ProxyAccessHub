using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Core.Authentication;

namespace ProxyAccessHub.Pages.User;

/// <summary>
/// Страница входа обычного пользователя.
/// </summary>
public sealed class LoginModel : PageModel
{
    private readonly UserAccessOptions userAccessOptions;

    /// <summary>
    /// Инициализирует страницу входа обычного пользователя.
    /// </summary>
    /// <param name="userAccessOptions">Настройки доступа обычных пользователей.</param>
    public LoginModel(IOptions<UserAccessOptions> userAccessOptions)
    {
        this.userAccessOptions = userAccessOptions.Value;
    }

    /// <summary>
    /// Введённый общий пароль.
    /// </summary>
    [BindProperty]
    [Display(Name = "Общий пароль")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Сообщение об ошибке входа.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/User/Renew");
        }

        return Page();
    }

    /// <summary>
    /// Обрабатывает отправку формы входа.
    /// </summary>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(userAccessOptions.SharedPassword))
        {
            throw new InvalidOperationException("В конфигурации не задан общий пользовательский пароль.");
        }

        if (!string.Equals(Password, userAccessOptions.SharedPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Неверный общий пароль.";
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

        return RedirectToPage("/User/Renew");
    }
}
