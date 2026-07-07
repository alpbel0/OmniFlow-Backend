using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTripDestinationCoordinatesGeocodingCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "trip_destinations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "trip_destinations",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "geocoding_cache_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    forward_key = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    reverse_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geocoding_cache_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_geocoding_cache_forward",
                table: "geocoding_cache_entries",
                columns: new[] { "provider", "forward_key" },
                unique: true,
                filter: "forward_key IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_geocoding_cache_reverse",
                table: "geocoding_cache_entries",
                columns: new[] { "provider", "reverse_key" },
                unique: true,
                filter: "reverse_key IS NOT NULL AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "geocoding_cache_entries");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "trip_destinations");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "trip_destinations");
        }
    }
}
