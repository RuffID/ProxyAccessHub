using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
