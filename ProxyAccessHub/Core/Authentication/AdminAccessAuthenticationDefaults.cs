namespace ProxyAccessHub.Core.Authentication;

/// <summary>
/// Константы cookie-аутентификации администратора.
/// </summary>
public static class AdminAccessAuthenticationDefaults
{
    /// <summary>
    /// Схема аутентификации администратора.
    /// </summary>
    public const string AUTHENTICATION_SCHEME = "AdminAccess";

    /// <summary>
    /// Имя cookie администратора.
    /// </summary>
    public const string COOKIE_NAME = ".ProxyAccessHub.AdminAccess";

    /// <summary>
    /// Путь страницы входа администратора.
    /// </summary>
    public const string LOGIN_PATH = "/login";

    /// <summary>
    /// Путь основной страницы администратора.
    /// </summary>
    public const string DEFAULT_SUCCESS_PATH = "/Admin/Users";

    /// <summary>
    /// Тип claim успешного входа администратора.
    /// </summary>
    public const string ACCESS_CLAIM_TYPE = "proxy_access_hub_admin_access";

    /// <summary>
    /// Значение claim успешного входа администратора.
    /// </summary>
    public const string ACCESS_CLAIM_VALUE = "granted";
}
