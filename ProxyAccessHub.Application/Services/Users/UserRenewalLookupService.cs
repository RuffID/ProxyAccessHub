using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Models.Tariffs;
using ProxyAccessHub.Application.Models.Users;
using ProxyAccessHub.Domain.Entities;

namespace ProxyAccessHub.Application.Services.Users;

/// <summary>
/// Ищет пользователя для экрана продления и рассчитывает сумму следующего периода.
/// </summary>
public class UserRenewalLookupService(
    IProxyAccessHubUnitOfWork unitOfWork,
    ITariffPriceResolver tariffPriceResolver) : IUserRenewalLookupService
{
    /// <inheritdoc />
    public async Task<UserRenewalLookupResult> FindAsync(string searchValue, CancellationToken cancellationToken = default)
    {
        string normalizedSearchValue = NormalizeSearchValue(searchValue);
        IReadOnlyList<ProxyUser> users = await unitOfWork.Users.GetAllAsync(cancellationToken);

        List<ProxyUser> telemtMatches = users
            .Where(user => string.Equals(user.TelemtUserId.Trim(), normalizedSearchValue, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (telemtMatches.Count > 1)
        {
            throw new InvalidOperationException("Найдено несколько пользователей с одинаковым telemt id.");
        }

        if (telemtMatches.Count == 1)
        {
            return await BuildResultAsync(telemtMatches[0], "telemt id", cancellationToken);
        }

        string? normalizedSecret = TryExtractProxySecret(normalizedSearchValue);
        List<ProxyUser> proxyMatches = users
            .Where(user => IsProxyMatch(user, normalizedSearchValue, normalizedSecret))
            .ToList();

        if (proxyMatches.Count == 0)
        {
            throw new KeyNotFoundException("Пользователь по указанному значению не найден.");
        }

        if (proxyMatches.Count > 1)
        {
            throw new InvalidOperationException("По указанному окончанию proxy-ссылки найдено несколько пользователей.");
        }

        return await BuildResultAsync(proxyMatches[0], "окончание proxy-ссылки", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserRenewalLookupResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ProxyUser user = await unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Пользователь для продления не найден.");

        return await BuildResultAsync(user, "локальный идентификатор", cancellationToken);
    }

    private async Task<UserRenewalLookupResult> BuildResultAsync(ProxyUser user, string searchKind, CancellationToken cancellationToken)
    {
        TariffDefinition tariff = await unitOfWork.Tariffs.GetByIdAsync(user.TariffId, cancellationToken)
            ?? throw new KeyNotFoundException($"Тариф '{user.TariffId}' не найден.");
        TariffUserPriceOverride? priceOverride = user.TariffSettings is null
            ? null
            : new TariffUserPriceOverride(user.TariffSettings.CustomPeriodPriceRub, user.TariffSettings.DiscountPercent);
        decimal amountRequiredRub = tariff.RequiresRenewal
            ? tariffPriceResolver.ResolvePeriodPrice(tariff, priceOverride)
            : 0m;

        return new UserRenewalLookupResult(
            user.Id,
            user.TelemtUserId,
            user.ProxyLink,
            tariff.Id,
            tariff.Name,
            user.BalanceRub,
            user.AccessPaidToUtc,
            tariff.IsUnlimited || user.IsUnlimited,
            tariff.PeriodMonths,
            amountRequiredRub,
            searchKind);
    }

    private static bool IsProxyMatch(ProxyUser user, string normalizedSearchValue, string? normalizedSecret)
    {
        string normalizedProxyLink = user.ProxyLink.Trim();
        string normalizedLookupKey = user.ProxyLinkLookupKey.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedSecret)
            && string.Equals(normalizedLookupKey, normalizedSecret, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(normalizedLookupKey, normalizedSearchValue, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedProxyLink.EndsWith(normalizedSearchValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSearchValue(string searchValue)
    {
        if (string.IsNullOrWhiteSpace(searchValue))
        {
            throw new InvalidOperationException("Введите telemt id или окончание proxy-ссылки.");
        }

        return searchValue.Trim();
    }

    private static string? TryExtractProxySecret(string searchValue)
    {
        if (!Uri.TryCreate(searchValue, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

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

        return null;
    }
}
