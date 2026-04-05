using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Восстанавливает отсутствующие тарифные колонки пользователя, если они были удалены вручную из базы.
    /// </summary>
    [Migration("20260405171000_RepairMissingUserTariffColumns")]
    public partial class RepairMissingUserTariffColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddDecimalColumnIfMissing(
                migrationBuilder,
                "Users",
                "CustomPeriodPriceRub",
                "decimal(18,2)");

            AddDecimalColumnIfMissing(
                migrationBuilder,
                "Users",
                "DiscountPercent",
                "decimal(5,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropColumnIfExists(migrationBuilder, "Users", "CustomPeriodPriceRub");
            DropColumnIfExists(migrationBuilder, "Users", "DiscountPercent");
        }

        /// <summary>
        /// Добавляет decimal-колонку только если она отсутствует.
        /// </summary>
        /// <param name="migrationBuilder">Построитель миграции.</param>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="columnName">Имя колонки.</param>
        /// <param name="sqlType">SQL-тип колонки.</param>
        private static void AddDecimalColumnIfMissing(MigrationBuilder migrationBuilder, string tableName, string columnName, string sqlType)
        {
            migrationBuilder.Sql($"""
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[{tableName}] ADD [{columnName}] {sqlType} NULL');
END
""");
        }

        /// <summary>
        /// Удаляет колонку только если она существует.
        /// </summary>
        /// <param name="migrationBuilder">Построитель миграции.</param>
        /// <param name="tableName">Имя таблицы.</param>
        /// <param name="columnName">Имя колонки.</param>
        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName)
        {
            migrationBuilder.Sql($"""
IF COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [dbo].[{tableName}] DROP COLUMN [{columnName}]');
END
""");
        }
    }
}
