using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProxyAccessHub.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTariffs01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tariffs",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "TariffCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "TariffCode",
                table: "Subscriptions");

            migrationBuilder.AddColumn<Guid>(
                name: "TariffId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Tariffs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TariffId",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tariffs",
                table: "Tariffs",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Tariffs",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Tariffs");

            migrationBuilder.DropColumn(
                name: "TariffId",
                table: "Subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "TariffCode",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Tariffs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TariffCode",
                table: "Subscriptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tariffs",
                table: "Tariffs",
                column: "Code");
        }
    }
}
