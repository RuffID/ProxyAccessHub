using ProxyAccessHub.Application;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Infrastructure;

namespace ProxyAccessHub.Core;

/// <summary>
/// Расширения для централизованной регистрации сервисов приложения.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует конфигурацию и базовые сервисы веб-приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="builder">Построитель веб-приложения.</param>
    /// <returns>Текущая коллекция сервисов.</returns>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddConfig(builder.Configuration);
        services.AddApplication();
        services.AddInfrastructure(builder.Configuration);
        services.AddRazorPages();

        return services;
    }

    /// <summary>
    /// Регистрирует конфигурационные секции приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов приложения.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <returns>Текущая коллекция сервисов.</returns>
    private static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProxyAccessHubOptions>(
            configuration.GetSection(ProxyAccessHubOptions.SECTION_NAME));
        services.Configure<YooMoneyOptions>(
            configuration.GetSection(YooMoneyOptions.SECTION_NAME));
        services.Configure<TelemtOptions>(
            configuration.GetSection(TelemtOptions.SECTION_NAME));

        return services;
    }
}
