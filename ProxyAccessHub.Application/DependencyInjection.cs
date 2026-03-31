using Microsoft.Extensions.DependencyInjection;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Abstractions.Subscriptions;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Tariffs;
using ProxyAccessHub.Application.Abstractions.Users;
using ProxyAccessHub.Application.Services.Payments;
using ProxyAccessHub.Application.Services.Subscriptions;
using ProxyAccessHub.Application.Services.Telemt;
using ProxyAccessHub.Application.Services.Tariffs;
using ProxyAccessHub.Application.Services.Users;

namespace ProxyAccessHub.Application;

/// <summary>
/// Расширения для регистрации зависимостей слоя приложения.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет зависимости слоя приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <returns>Текущая коллекция сервисов.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITariffCatalog, ConfiguredTariffCatalog>();
        services.AddSingleton<ITariffPriceResolver, TariffPriceResolver>();
        services.AddSingleton<ITariffRenewalCalculator, TariffRenewalCalculator>();
        services.AddScoped<IUserSubscriptionRenewalService, UserSubscriptionRenewalService>();
        services.AddScoped<ITelemtUsersSyncService, TelemtUsersSyncService>();
        services.AddScoped<IUserPaymentRequestService, UserPaymentRequestService>();
        services.AddScoped<IYooMoneyNotificationService, YooMoneyNotificationService>();
        services.AddScoped<IUserRenewalLookupService, UserRenewalLookupService>();
        services.AddScoped<IUserConnectionCreationService, UserConnectionCreationService>();
        services.AddScoped<IAdminUserManagementService, AdminUserManagementService>();

        return services;
    }
}
