using EFCoreLibrary.Abstractions.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ProxyAccessHub.Infrastructure.Service.DataBase;

/// <summary>
/// Проверяет доступность базы данных и применяет ожидающие миграции.
/// </summary>
/// <typeparam name="TContext">Тип контекста базы данных.</typeparam>
public class DataBaseCheckUpService<TContext>(
    IAppDbContext<TContext> dbContext,
    ILoggerFactory loggerFactory,
    BackupService<TContext> backupService)
    where TContext : DbContext
{
    private readonly ILogger<DataBaseCheckUpService<TContext>> logger = loggerFactory.CreateLogger<DataBaseCheckUpService<TContext>>();

    /// <summary>
    /// Проверяет подключение к базе данных и при необходимости применяет миграции.
    /// </summary>
    public void CheckOrUpdateDb()
    {
        if (!dbContext.Database.CanConnect())
        {
            logger.LogError("[Method:{MethodName}] Failed to connect to the database.", nameof(CheckOrUpdateDb));
            throw new InvalidOperationException("Не удалось установить соединение с базой данных.");
        }

        logger.LogInformation("[Method:{MethodName}] Connection to the database was successful.", nameof(CheckOrUpdateDb));

        List<string> pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count == 0)
        {
            logger.LogInformation("[Method:{MethodName}] No changes to the database.", nameof(CheckOrUpdateDb));
            return;
        }

        foreach (string migration in pendingMigrations)
        {
            logger.LogInformation("[Method:{MethodName}] Pending migration: {Migration}.", nameof(CheckOrUpdateDb), migration);
        }

        backupService.CreateSqlServerBackup();
        dbContext.Database.Migrate();
        logger.LogInformation("[Method:{MethodName}] Database was updated.", nameof(CheckOrUpdateDb));
    }
}
