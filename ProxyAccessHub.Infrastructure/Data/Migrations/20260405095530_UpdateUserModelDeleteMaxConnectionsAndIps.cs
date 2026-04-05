using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModelDeleteMaxConnectionsAndIps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropColumnIfExists(migrationBuilder, "Users", "ActiveUniqueIps");
            DropColumnIfExists(migrationBuilder, "Users", "CurrentConnections");
            DropColumnIfExists(migrationBuilder, "Users", "DataQuotaBytes");
            DropColumnIfExists(migrationBuilder, "Users", "IsUnlimited");
            DropColumnIfExists(migrationBuilder, "Users", "MaxTcpConnections");
            DropColumnIfExists(migrationBuilder, "Users", "MaxUniqueIps");
            DropColumnIfExists(migrationBuilder, "Users", "TotalOctets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActiveUniqueIps",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentConnections",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "DataQuotaBytes",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnlimited",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxTcpConnections",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUniqueIps",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalOctets",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <summary>
        /// Удаляет колонку только если она ещё существует в таблице.
        /// </summary>
        /// <param name="migrationBuilder">Построитель миграции.</param>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="columnName">Имя колонки.</param>
        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            migrationBuilder.Sql($"""
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NOT NULL
BEGIN
    DECLARE @constraintName nvarchar(128);
    SELECT @constraintName = [d].[name]
    FROM [sys].[default_constraints] AS [d]
    INNER JOIN [sys].[columns] AS [c]
        ON [d].[parent_column_id] = [c].[column_id]
       AND [d].[parent_object_id] = [c].[object_id]
    WHERE [d].[parent_object_id] = OBJECT_ID(N'[dbo].[{tableName}]')
      AND [c].[name] = N'{columnName}';

    IF @constraintName IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP CONSTRAINT [' + @constraintName + N']');

    EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP COLUMN [{columnName}]');
END
""");
        }
    }
}
