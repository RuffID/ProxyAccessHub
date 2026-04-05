using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTariffAssignmentsTrial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTariffAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TariffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false),
                    TrialDurationDays = table.Column<int>(type: "int", nullable: true),
                    NextTariffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AssignedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTariffAssignments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTariffAssignments_IsTrial_EndedAtUtc",
                table: "UserTariffAssignments",
                columns: new[] { "IsTrial", "EndedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTariffAssignments_UserId",
                table: "UserTariffAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTariffAssignments_UserId_EndedAtUtc",
                table: "UserTariffAssignments",
                columns: new[] { "UserId", "EndedAtUtc" },
                unique: true,
                filter: "[EndedAtUtc] IS NULL");

            migrationBuilder.Sql(
                """
                INSERT INTO [UserTariffAssignments] (
                    [Id],
                    [UserId],
                    [TariffId],
                    [StartedAtUtc],
                    [EndedAtUtc],
                    [IsTrial],
                    [TrialDurationDays],
                    [NextTariffId],
                    [CreatedAtUtc],
                    [Comment],
                    [AssignedBy])
                SELECT
                    NEWID(),
                    [Id],
                    [TariffId],
                    [LastSyncedAtUtc],
                    NULL,
                    0,
                    NULL,
                    NULL,
                    [LastSyncedAtUtc],
                    N'Начальное назначение тарифа, перенесённое миграцией.',
                    N'migration'
                FROM [Users]
                WHERE NOT (
                    [TelemtRevision] = N'pending-create'
                    AND [ProxyLink] LIKE N'pending://create/%'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTariffAssignments");
        }
    }
}
