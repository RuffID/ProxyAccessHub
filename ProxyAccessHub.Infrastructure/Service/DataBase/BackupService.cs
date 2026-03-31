using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace ProxyAccessHub.Infrastructure.Service.DataBase;

/// <summary>
/// Создаёт резервную копию SQL Server перед применением миграций.
/// </summary>
/// <typeparam name="TContext">Тип контекста базы данных.</typeparam>
public class BackupService<TContext>(string connectionString, string backupFolder, ILogger<BackupService<TContext>> logger)
    where TContext : DbContext
{
    /// <summary>
    /// Создаёт резервную копию базы данных SQL Server.
    /// </summary>
    public void CreateSqlServerBackup()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        string timestamp = DateTime.Now.ToString("yyyy.MM.dd_HHmmss");
        string backupFilePath = Path.Combine(backupFolder, $"backup_{timestamp}.bak");

        using SqlConnection connection = new(connectionString);
        connection.Open();

        string databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        string sql = $"BACKUP DATABASE [{databaseName}] TO DISK = N'{backupFilePath}' WITH FORMAT, INIT, NAME = 'Scheduled Backup';";

        using SqlCommand command = new(sql, connection);
        command.ExecuteNonQuery();

        logger.LogInformation("[Method:{MethodName}] Backup created at: {BackupFilePath}", nameof(CreateSqlServerBackup), backupFilePath);
    }
}
