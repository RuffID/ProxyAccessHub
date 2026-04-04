using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerApiSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiBearerToken",
                table: "Servers",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApiPort",
                table: "Servers",
                type: "int",
                nullable: false,
                defaultValue: 9091);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiBearerToken",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "ApiPort",
                table: "Servers");
        }
    }
}
