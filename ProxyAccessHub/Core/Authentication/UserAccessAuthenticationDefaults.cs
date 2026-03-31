namespace ProxyAccessHub.Core.Authentication;

/// <summary>
/// Константы cookie-аутентификации обычного пользователя.
/// </summary>
public static class UserAccessAuthenticationDefaults
{
    /// <summary>
    /// Схема аутентификации обычного пользователя.
    /// </summary>
    public const string AUTHENTICATION_SCHEME = "UserAccess";

    /// <summary>
    /// Тип claim успешного пользовательского входа.
    /// </summary>
    public const string ACCESS_CLAIM_TYPE = "proxy_access_hub_user_access";

    /// <summary>
    /// Значение claim успешного пользовательского входа.
    /// </summary>
    public const string ACCESS_CLAIM_VALUE = "granted";
}
