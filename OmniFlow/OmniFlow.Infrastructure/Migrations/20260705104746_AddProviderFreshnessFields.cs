using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderFreshnessFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "data_snapshot_date",
                table: "provider_hotels",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.AddColumn<bool>(
                name: "is_live_data",
                table: "provider_hotels",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_updated_at",
                table: "provider_hotels",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "(now() at time zone 'utc')");

            migrationBuilder.AddColumn<DateOnly>(
                name: "data_snapshot_date",
                table: "provider_flights",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");

            migrationBuilder.AddColumn<bool>(
                name: "is_live_data",
                table: "provider_flights",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_updated_at",
                table: "provider_flights",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "(now() at time zone 'utc')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_snapshot_date",
                table: "provider_hotels");

            migrationBuilder.DropColumn(
                name: "is_live_data",
                table: "provider_hotels");

            migrationBuilder.DropColumn(
                name: "last_updated_at",
                table: "provider_hotels");

            migrationBuilder.DropColumn(
                name: "data_snapshot_date",
                table: "provider_flights");

            migrationBuilder.DropColumn(
                name: "is_live_data",
                table: "provider_flights");

            migrationBuilder.DropColumn(
                name: "last_updated_at",
                table: "provider_flights");
        }
    }
}
