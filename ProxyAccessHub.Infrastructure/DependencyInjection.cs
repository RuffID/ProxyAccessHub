using EFCoreLibrary.Abstractions.Database;
using EFCoreLibrary.EfCore;
using EFCoreLibrary.Extensions;
using HttpClientLibrary;
using HttpClientLibrary.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ProxyAccessHub.Application.Abstractions.Telemt;
using ProxyAccessHub.Application.Abstractions.Storage;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Storage.SqlServer;
using ProxyAccessHub.Infrastructure.Telemt;

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
        TelemtOptions telemtOptions = configuration.GetSection(TelemtOptions.SECTION_NAME).Get<TelemtOptions>()
            ?? throw new InvalidOperationException("Не удалось прочитать секцию настроек telemt.");

        services.AddDbContext<ProxyAccessHubDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
        services.AddScoped<IAppDbContext<ProxyAccessHubDbContext>>(serviceProvider =>
            new EfDbContextAdapter<ProxyAccessHubDbContext>(serviceProvider.GetRequiredService<ProxyAccessHubDbContext>()));
        services.AddEfCoreBaseRepositories<ProxyAccessHubDbContext>();

        services.AddHttpClient<IHttpApiClient, HttpApiClient>(httpClient =>
        {
            if (!Uri.TryCreate(telemtOptions.ApiBaseUrl, UriKind.Absolute, out Uri? apiUri))
            {
                throw new InvalidOperationException("Адрес telemt API должен быть задан абсолютным URL.");
            }

            string basePath = apiUri.AbsolutePath.TrimEnd('/');
            string normalizedBasePath = string.Equals(basePath, "/v1", StringComparison.OrdinalIgnoreCase)
                ? "/v1/"
                : string.IsNullOrEmpty(basePath) || basePath == "/"
                    ? "/v1/"
                    : $"{basePath}/v1/";

            Uri baseAddress = new(apiUri.GetLeftPart(UriPartial.Authority) + normalizedBasePath, UriKind.Absolute);

            httpClient.BaseAddress = baseAddress;
            httpClient.Timeout = TimeSpan.FromSeconds(180);
        });
        services.AddScoped<ITelemtApiClient, TelemtApiClient>();
        services.AddSingleton<ITelemtSyncStateStore, TelemtSyncStateStore>();

        services.AddScoped<IProxyUserRepository, ProxyUserRepository>();
        services.AddScoped<IProxyServerRepository, ProxyServerRepository>();
        services.AddScoped<ITariffDefinitionRepository, TariffDefinitionRepository>();
        services.AddScoped<IPaymentRequestRepository, PaymentRequestRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IProxyAccessHubUnitOfWork, ProxyAccessHubUnitOfWork>();

        if (telemtOptions.SyncEnabled)
        {
            services.AddHostedService<TelemtUsersSyncBackgroundService>();
        }

        return services;
    }
}
