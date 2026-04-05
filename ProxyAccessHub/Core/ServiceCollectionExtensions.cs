using ProxyAccessHub.Application;
using ProxyAccessHub.Application.Configuration;
using ProxyAccessHub.Core.Authentication;
using ProxyAccessHub.Infrastructure;
using ProxyAccessHub.Infrastructure.Data;
using ProxyAccessHub.Infrastructure.Service.DataBase;
using Microsoft.AspNetCore.DataProtection;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ProxyAccessHub.Core;

/// <summary>
/// Расширения для централизованной регистрации сервисов приложения.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует конфигурацию и базовые сервисы веб-приложения.
    /// </summary>
    public static IServiceCollection ConfigureServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddConfig(builder.Configuration);
        services.AddApplication();
        services.AddInfrastructure(builder.Configuration);
        services.AddDataProtection(builder);
        services.AddDatabaseMaintenance(builder.Configuration);
        services.AddAuthentication(UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME)
            .AddCookie(UserAccessAuthenticationDefaults.AUTHENTICATION_SCHEME, options =>
            {
                options.Cookie.Name = ".ProxyAccessHub.UserAccess";
                options.LoginPath = "/";
                options.AccessDeniedPath = "/";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
            })
            .AddCookie(AdminAccessAuthenticationDefaults.AUTHENTICATION_SCHEME, options =>
            {
                options.Cookie.Name = AdminAccessAuthenticationDefaults.COOKIE_NAME;
                options.LoginPath = AdminAccessAuthenticationDefaults.LOGIN_PATH;
                options.AccessDeniedPath = AdminAccessAuthenticationDefaults.LOGIN_PATH;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
            });
        services.AddAuthorization();
        services.AddControllers();
        services.AddRazorPages();

        return services;
    }

    /// <summary>
    /// Регистрирует настройки защиты данных приложения.
    /// </summary>
    private static IServiceCollection AddDataProtection(this IServiceCollection services, WebApplicationBuilder builder)
    {
        string keyPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(builder.Environment.ContentRootPath, "keys-windows")
            : Path.Combine(builder.Environment.ContentRootPath, "keys-linux");
        string applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "ProxyAccessHub";

        Directory.CreateDirectory(keyPath);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
            .SetApplicationName(applicationName);

        return services;
    }

    /// <summary>
    /// Регистрирует сервисы обслуживания базы данных.
    /// </summary>
    private static IServiceCollection AddDatabaseMaintenance(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("MSSql")
            ?? throw new InvalidOperationException("Не задана строка подключения 'ConnectionStrings:MSSql'.");

        services.AddScoped<DataBaseCheckUpService<ProxyAccessHubDbContext>>();
        services.AddScoped(serviceProvider =>
        {
            ILogger<BackupService<ProxyAccessHubDbContext>> logger = serviceProvider.GetRequiredService<ILogger<BackupService<ProxyAccessHubDbContext>>>();
            string backupFolder = OperatingSystem.IsLinux()
                ? "/var/opt/mssql/backups"
                : Path.Combine(AppContext.BaseDirectory, "Backups");

            return new BackupService<ProxyAccessHubDbContext>(connectionString, backupFolder, logger);
        });

        return services;
    }

    /// <summary>
    /// Регистрирует конфигурационные секции приложения.
    /// </summary>
    private static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProxyAccessHubOptions>(
            configuration.GetSection(ProxyAccessHubOptions.SECTION_NAME));
        services.Configure<UserAccessOptions>(
            configuration.GetSection(UserAccessOptions.SECTION_NAME));
        services.Configure<AdminAccessOptions>(
            configuration.GetSection(AdminAccessOptions.SECTION_NAME));
        services.Configure<YooMoneyOptions>(
            configuration.GetSection(YooMoneyOptions.SECTION_NAME));
        return services;
    }
}
