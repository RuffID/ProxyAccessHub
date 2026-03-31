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
public sealed class UsersModel : PageModel
{
    private readonly IAdminUserManagementService adminUserManagementService;

    /// <summary>
    /// Инициализирует страницу списка пользователей администратора.
    /// </summary>
    /// <param name="adminUserManagementService">Сервис минимального управления пользователями из админки.</param>
    public UsersModel(IAdminUserManagementService adminUserManagementService)
    {
        this.adminUserManagementService = adminUserManagementService;
    }

    /// <summary>
    /// Данные страницы пользователей для отображения в админке.
    /// </summary>
    public AdminUsersPageData PageData { get; private set; } = new(
        [],
        [],
        new AdminTelemtSyncStatus(
            "Ожидание синхронизации",
            "Фоновая синхронизация telemt ещё не выполнялась.",
            null,
            null,
            null,
            null,
            null,
            null,
            null));

    /// <summary>
    /// Показывает только пользователей с активной ручной обработкой.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool OnlyManualHandling { get; set; }

    /// <summary>
    /// Сообщение об успешном обновлении пользователя.
    /// </summary>
    public string StatusMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Сообщение об ошибке обновления пользователя.
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Обрабатывает открытие страницы.
    /// </summary>
    public async Task OnGetAsync()
    {
        PageData = await adminUserManagementService.GetPageDataAsync(OnlyManualHandling, HttpContext.RequestAborted);
    }

    /// <summary>
    /// Обновляет тариф пользователя и индивидуальные настройки цены.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="tariffCode">Код выбранного тарифа.</param>
    /// <param name="customPeriodPriceRub">Индивидуальная цена периода в рублях.</param>
    /// <param name="discountPercent">Индивидуальная скидка в процентах.</param>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostUpdateAsync(
        Guid userId,
        string tariffCode,
        decimal? customPeriodPriceRub,
        decimal? discountPercent,
        bool onlyManualHandling)
    {
        OnlyManualHandling = onlyManualHandling;

        if (!ModelState.IsValid)
        {
            ErrorMessage = GetModelStateErrorMessage();
            PageData = await adminUserManagementService.GetPageDataAsync(OnlyManualHandling, HttpContext.RequestAborted);
            return Page();
        }

        try
        {
            await adminUserManagementService.UpdateUserTariffAsync(
                userId,
                tariffCode,
                customPeriodPriceRub,
                discountPercent,
                HttpContext.RequestAborted);

            StatusMessage = "Настройки пользователя сохранены.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }

        PageData = await adminUserManagementService.GetPageDataAsync(OnlyManualHandling, HttpContext.RequestAborted);
        return Page();
    }

    /// <summary>
    /// Помечает ручную обработку пользователя как завершённую.
    /// </summary>
    /// <param name="userId">Локальный идентификатор пользователя.</param>
    /// <param name="onlyManualHandling">Признак фильтрации только проблемных кейсов.</param>
    /// <returns>Текущий результат обработки запроса.</returns>
    public async Task<IActionResult> OnPostCompleteManualHandlingAsync(Guid userId, bool onlyManualHandling)
    {
        OnlyManualHandling = onlyManualHandling;

        try
        {
            await adminUserManagementService.CompleteManualHandlingAsync(userId, HttpContext.RequestAborted);
            StatusMessage = "Ручная обработка пользователя отмечена как завершённая.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            ErrorMessage = ex.Message;
        }

        PageData = await adminUserManagementService.GetPageDataAsync(OnlyManualHandling, HttpContext.RequestAborted);
        return Page();
    }

    private string GetModelStateErrorMessage()
    {
        string? errorMessage = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));

        return string.IsNullOrWhiteSpace(errorMessage)
            ? "Проверьте корректность введённых значений."
            : errorMessage;
    }
}
