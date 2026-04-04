using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tariffs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Tariffs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Servers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Servers");
        }
    }
}
