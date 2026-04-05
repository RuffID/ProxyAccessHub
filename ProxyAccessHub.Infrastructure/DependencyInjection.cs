using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.EfCore;
using EFCoreLibrary.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Payments;
using ProxyAccessHub.Infrastructure.Storage.SqlServer;
using ProxyAccessHub.Infrastructure.Telemt;
using ProxyAccessHub.Infrastructure.Users;

namespace ProxyAccessHub.Infrastructure;

/// <summary>
/// Расширения для регистрации зависимостей инфраструктурного слоя.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Добавляет зависимости инфраструктурного слоя.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Текущая коллекция сервисов.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("MSSql")
            ?? throw new InvalidOperationException("Не задана строка подключения 'ConnectionStrings:MSSql'.");

        services.AddDbContext<ProxyAccessHubDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        services.AddScoped<IAppDbContext<ProxyAccessHubDbContext>>(serviceProvider =>
            new EfDbContextAdapter<ProxyAccessHubDbContext>(serviceProvider.GetRequiredService<ProxyAccessHubDbContext>()));
        services.AddEfCoreBaseRepositories<ProxyAccessHubDbContext>();

        services.AddHttpClient();
        services.AddScoped<ITelemtApiClient, TelemtApiClient>();
        services.AddScoped<IYooMoneyWalletClient, YooMoneyWalletClient>();
        services.AddSingleton<IYooMoneySettingsStore, YooMoneySettingsStore>();
        services.AddSingleton<ITelemtSyncStateStore, TelemtSyncStateStore>();

        services.AddScoped<IProxyUserRepository, ProxyUserRepository>();
        services.AddScoped<IProxyServerRepository, ProxyServerRepository>();
        services.AddScoped<ITariffDefinitionRepository, TariffDefinitionRepository>();
        services.AddScoped<IPaymentRequestRepository, PaymentRequestRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUserTariffAssignmentRepository, UserTariffAssignmentRepository>();
        services.AddScoped<IProxyAccessHubUnitOfWork, ProxyAccessHubUnitOfWork>();

        services.AddHostedService<TelemtUsersSyncBackgroundService>();
        services.AddHostedService<TrialTariffTransitionBackgroundService>();
        services.AddHostedService<ScheduledRenewalBackgroundService>();

        return services;
    }
}
