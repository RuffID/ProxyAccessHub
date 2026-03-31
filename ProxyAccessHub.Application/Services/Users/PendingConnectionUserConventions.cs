using ProxyAccessHub.Application.Models.Telemt;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Содержит соглашения по локальному состоянию ожидающего нового подключения.
/// </summary>
internal static class PendingConnectionUserConventions
{
    private const string USERNAME_PREFIX = "pah-";
    private const string PENDING_PROXY_LINK_PREFIX = "pending://create/";
    private const string PENDING_PROXY_LOOKUP_KEY_PREFIX = "pending-";
    private const string PENDING_TELEMT_REVISION = "pending-create";

    /// <summary>
    /// Генерирует идентификатор пользователя для telemt.
    /// </summary>
    /// <returns>Уникальный идентификатор пользователя.</returns>
    public static string GenerateTelemtUserId()
    {
        return USERNAME_PREFIX + Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Возвращает признак локального ожидающего пользователя нового подключения.
    /// </summary>
    /// <param name="user">Проверяемый пользователь.</param>
    /// <returns><see langword="true" />, если пользователь ещё не создан в telemt.</returns>
    public static bool IsPending(ProxyUser user)
    {
        return user.ProxyLink.StartsWith(PENDING_PROXY_LINK_PREFIX, StringComparison.OrdinalIgnoreCase)
            && string.Equals(user.TelemtRevision, PENDING_TELEMT_REVISION, StringComparison.Ordinal);
    }

    /// <summary>
    /// Создаёт временную proxy-ссылку для локального ожидающего пользователя.
    /// </summary>
    /// <param name="telemtUserId">Идентификатор пользователя в telemt.</param>
    /// <returns>Временная proxy-ссылка.</returns>
    public static string BuildPendingProxyLink(string telemtUserId)
    {
        return PENDING_PROXY_LINK_PREFIX + telemtUserId;
    }

    /// <summary>
    /// Создаёт временный ключ поиска по proxy-ссылке для локального ожидающего пользователя.
    /// </summary>
    /// <returns>Временный ключ поиска.</returns>
    public static string BuildPendingProxyLookupKey()
    {
        return PENDING_PROXY_LOOKUP_KEY_PREFIX + Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Возвращает техническую ревизию для локального ожидающего пользователя.
    /// </summary>
    /// <returns>Техническая ревизия ожидающего пользователя.</returns>
    public static string GetPendingRevision()
    {
        return PENDING_TELEMT_REVISION;
    }

    /// <summary>
    /// Выбирает основную proxy-ссылку telemt.
    /// </summary>
    /// <param name="links">Набор ссылок пользователя.</param>
    /// <returns>Основная proxy-ссылка пользователя.</returns>
    public static string SelectPrimaryProxyLink(TelemtUserLinks links)
    {
        string? secureLink = SelectFirstLink(links.Secure);
        if (secureLink is not null)
        {
            return secureLink;
        }

        string? tlsLink = SelectFirstLink(links.Tls);
        if (tlsLink is not null)
        {
            return tlsLink;
        }

        string? classicLink = SelectFirstLink(links.Classic);
        if (classicLink is not null)
        {
            return classicLink;
        }

        throw new InvalidOperationException("Telemt не вернул ни одной proxy-ссылки для пользователя.");
    }

    /// <summary>
    /// Строит ключ поиска по proxy-ссылке на основе параметра secret.
    /// </summary>
    /// <param name="proxyLink">Полная proxy-ссылка пользователя.</param>
    /// <returns>Нормализованный ключ поиска.</returns>
    public static string BuildProxyLookupKey(string proxyLink)
    {
        Uri uri = new(proxyLink);
        string query = uri.Query.TrimStart('?');
        string[] parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            string[] pair = part.Split('=', 2);

            if (pair.Length == 2 && string.Equals(pair[0], "secret", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(pair[1]))
                {
                    throw new InvalidOperationException("В proxy-ссылке отсутствует значение параметра secret.");
                }

                return Uri.UnescapeDataString(pair[1]).Trim().ToLowerInvariant();
            }
        }

        throw new InvalidOperationException("В proxy-ссылке отсутствует параметр secret.");
    }

    /// <summary>
    /// Возвращает первую непустую ссылку из набора.
    /// </summary>
    /// <param name="links">Проверяемый набор ссылок.</param>
    /// <returns>Первая непустая ссылка или <see langword="null" />.</returns>
    private static string? SelectFirstLink(IReadOnlyList<string> links)
    {
        foreach (string link in links)
        {
            if (!string.IsNullOrWhiteSpace(link))
            {
                return link.Trim();
            }
        }

        return null;
    }
}
