using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmountRub",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualAmountRub",
                table: "Payments");
        }
    }
}
