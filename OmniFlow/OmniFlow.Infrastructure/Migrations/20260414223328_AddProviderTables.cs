using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_flights",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flight_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    airline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    airline_logo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    departure_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    arrival_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    departure_airport_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    arrival_airport_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    departure_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    arrival_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    available_seats = table.Column<int>(type: "integer", nullable: true),
                    provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_flights", x => x.id);
                    table.CheckConstraint("valid_provider_flight_arrival_airport", "arrival_airport_code ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("valid_provider_flight_currency", "char_length(currency_code) = 3");
                    table.CheckConstraint("valid_provider_flight_departure_airport", "departure_airport_code ~ '^[A-Z]{3}$'");
                    table.CheckConstraint("valid_provider_flight_duration", "duration_minutes > 0");
                    table.CheckConstraint("valid_provider_flight_price", "price >= 0");
                    table.CheckConstraint("valid_provider_flight_seats", "available_seats IS NULL OR available_seats >= 0");
                    table.CheckConstraint("valid_provider_flight_times", "arrival_time > departure_time");
                });

            migrationBuilder.CreateTable(
                name: "provider_hotels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hotel_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    stars = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<double>(type: "double precision", nullable: true),
                    review_count = table.Column<int>(type: "integer", nullable: true),
                    valid_date = table.Column<DateOnly>(type: "date", nullable: false),
                    price_per_night = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_hotels", x => x.id);
                    table.CheckConstraint("valid_provider_hotel_currency", "char_length(currency_code) = 3");
                    table.CheckConstraint("valid_provider_hotel_price", "price_per_night >= 0");
                    table.CheckConstraint("valid_provider_hotel_rating", "rating IS NULL OR (rating >= 0 AND rating <= 10)");
                    table.CheckConstraint("valid_provider_hotel_review_count", "review_count IS NULL OR review_count >= 0");
                    table.CheckConstraint("valid_provider_hotel_stars", "stars IS NULL OR (stars >= 1 AND stars <= 5)");
                });

            migrationBuilder.CreateIndex(
                name: "idx_provider_flights_provider_name",
                table: "provider_flights",
                column: "provider_name");

            migrationBuilder.CreateIndex(
                name: "idx_provider_flights_route_departure_time",
                table: "provider_flights",
                columns: new[] { "departure_city", "arrival_city", "departure_time" });

            migrationBuilder.CreateIndex(
                name: "idx_provider_hotels_city_country_date",
                table: "provider_hotels",
                columns: new[] { "city", "country", "valid_date" });

            migrationBuilder.CreateIndex(
                name: "idx_provider_hotels_is_available",
                table: "provider_hotels",
                column: "is_available");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_flights");

            migrationBuilder.DropTable(
                name: "provider_hotels");
        }
    }
}
