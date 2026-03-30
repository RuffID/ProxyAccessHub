using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
