using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TripPlanningV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stops");

            migrationBuilder.RenameColumn(
                name: "user_budget",
                table: "trips",
                newName: "manual_budget");

            migrationBuilder.DropColumn(
                name: "travel_style",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "country",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "city",
                table: "trips");

            migrationBuilder.RenameColumn(
                name: "Wikipedia",
                table: "places",
                newName: "wikipedia");

            migrationBuilder.RenameColumn(
                name: "Wikidata",
                table: "places",
                newName: "wikidata");

            migrationBuilder.RenameColumn(
                name: "Wheelchair",
                table: "places",
                newName: "wheelchair");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "places",
                newName: "image");

            migrationBuilder.RenameColumn(
                name: "Heritage",
                table: "places",
                newName: "heritage");

            migrationBuilder.RenameColumn(
                name: "Fee",
                table: "places",
                newName: "fee");

            migrationBuilder.RenameColumn(
                name: "Cuisine",
                table: "places",
                newName: "cuisine");

            migrationBuilder.RenameColumn(
                name: "ReviewCount",
                table: "places",
                newName: "review_count");

            migrationBuilder.RenameColumn(
                name: "PriceLevel",
                table: "places",
                newName: "price_level");

            migrationBuilder.RenameColumn(
                name: "PhotoUrls",
                table: "places",
                newName: "photo_urls");

            migrationBuilder.AddColumn<string>(
                name: "adjusted_budget_tier",
                table: "trips",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "origin_country",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "travel_styles",
                table: "trips",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "travel_companion",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tempo",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "transport_preference",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE places 
                SET photo_urls = (
                    SELECT array_agg(elem::text)
                    FROM jsonb_array_elements_text(photo_urls::jsonb) AS elem
                )
                WHERE photo_urls IS NOT NULL AND photo_urls LIKE '[%';
            ");

            migrationBuilder.Sql(@"ALTER TABLE places ALTER COLUMN photo_urls TYPE text[] USING photo_urls::text[];");

            migrationBuilder.AlterColumn<string[]>(
                name: "photo_urls",
                table: "places",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0],
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "google_tags",
                table: "places",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.CreateTable(
                name: "trip_destinations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    arrival_date = table.Column<DateOnly>(type: "date", nullable: false),
                    departure_date = table.Column<DateOnly>(type: "date", nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    night_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trip_destinations", x => x.id);
                    table.CheckConstraint("valid_dates", "departure_date >= arrival_date");
                    table.CheckConstraint("valid_night_count", "night_count >= 0");
                    table.CheckConstraint("valid_order_index", "order_index BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_trip_destinations_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "timeline_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    entry_type = table.Column<string>(type: "text", nullable: false),
                    order_index = table.Column<double>(type: "double precision", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    custom_name = table.Column<string>(type: "text", nullable: true),
                    custom_category = table.Column<string>(type: "text", nullable: true),
                    custom_photo_url = table.Column<string>(type: "text", nullable: true),
                    custom_latitude = table.Column<double>(type: "double precision", nullable: true),
                    custom_longitude = table.Column<double>(type: "double precision", nullable: true),
                    custom_description = table.Column<string>(type: "text", nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    buffer_minutes = table.Column<int>(type: "integer", nullable: true),
                    flight_from_airport = table.Column<string>(type: "text", nullable: true),
                    flight_to_airport = table.Column<string>(type: "text", nullable: true),
                    flight_from_city = table.Column<string>(type: "text", nullable: true),
                    flight_to_city = table.Column<string>(type: "text", nullable: true),
                    flight_departure_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    flight_arrival_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    airline = table.Column<string>(type: "text", nullable: true),
                    flight_number = table.Column<string>(type: "text", nullable: true),
                    transport_type = table.Column<string>(type: "text", nullable: true),
                    transport_from_station = table.Column<string>(type: "text", nullable: true),
                    transport_to_station = table.Column<string>(type: "text", nullable: true),
                    transport_company = table.Column<string>(type: "text", nullable: true),
                    accommodation_check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accommodation_check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accommodation_address = table.Column<string>(type: "text", nullable: true),
                    price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    currency_code = table.Column<string>(type: "text", nullable: false, defaultValue: "USD"),
                    provider_flight_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider_hotel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_visited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    added_by = table.Column<string>(type: "text", nullable: false),
                    ai_reasoning = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timeline_entries", x => x.id);
                    table.CheckConstraint("custom_accommodation_requires_dates", "entry_type != 'CustomAccommodation' OR (accommodation_check_in IS NOT NULL AND accommodation_check_out IS NOT NULL)");
                    table.CheckConstraint("custom_event_requires_time", "entry_type != 'CustomEvent' OR (start_time IS NOT NULL AND duration_minutes IS NOT NULL)");
                    table.CheckConstraint("custom_flight_requires_fields", "entry_type != 'CustomFlight' OR (flight_from_airport IS NOT NULL AND flight_to_airport IS NOT NULL AND flight_departure_at IS NOT NULL AND flight_arrival_at IS NOT NULL)");
                    table.CheckConstraint("custom_transport_requires_type", "entry_type != 'CustomTransport' OR transport_type IS NOT NULL");
                    table.CheckConstraint("entry_type_place_requires_id", "entry_type = 'Place' AND place_id IS NOT NULL OR entry_type != 'Place'");
                    table.CheckConstraint("locked_entry_has_buffer", "is_locked = FALSE OR buffer_minutes IS NOT NULL");
                    table.CheckConstraint("valid_order_index", "order_index > 0");
                    table.ForeignKey(
                        name: "FK_timeline_entries_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_timeline_entries_provider_flights_provider_flight_id",
                        column: x => x.provider_flight_id,
                        principalTable: "provider_flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_timeline_entries_provider_hotels_provider_hotel_id",
                        column: x => x.provider_hotel_id,
                        principalTable: "provider_hotels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_timeline_entries_trip_destinations_destination_id",
                        column: x => x.destination_id,
                        principalTable: "trip_destinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_timeline_entries_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_places_google_tags_gin",
                table: "places",
                column: "google_tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_timeline_entries_destination_id",
                table: "timeline_entries",
                column: "destination_id");

            migrationBuilder.CreateIndex(
                name: "idx_timeline_entries_place",
                table: "timeline_entries",
                column: "place_id",
                filter: "place_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_timeline_entries_provider_flight",
                table: "timeline_entries",
                column: "provider_flight_id",
                filter: "provider_flight_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_timeline_entries_provider_hotel",
                table: "timeline_entries",
                column: "provider_hotel_id",
                filter: "provider_hotel_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_timeline_entries_trip_dest_day_order",
                table: "timeline_entries",
                columns: new[] { "trip_id", "destination_id", "day_number", "order_index" });

            migrationBuilder.CreateIndex(
                name: "idx_trip_destinations_city",
                table: "trip_destinations",
                column: "city",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_trip_destinations_trip_order",
                table: "trip_destinations",
                columns: new[] { "trip_id", "order_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timeline_entries");

            migrationBuilder.DropTable(
                name: "trip_destinations");

            migrationBuilder.DropIndex(
                name: "idx_places_google_tags_gin",
                table: "places");

            migrationBuilder.DropColumn(
                name: "adjusted_budget_tier",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "origin_country",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "travel_styles",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "google_tags",
                table: "places");

            migrationBuilder.DropColumn(
                name: "travel_companion",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "transport_preference",
                table: "trips");

            migrationBuilder.DropColumn(
                name: "tempo",
                table: "trips");

            migrationBuilder.AddColumn<string>(
                name: "travel_style",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "trips",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "manual_budget",
                table: "trips",
                newName: "user_budget");

            migrationBuilder.RenameColumn(
                name: "wikipedia",
                table: "places",
                newName: "Wikipedia");

            migrationBuilder.RenameColumn(
                name: "wikidata",
                table: "places",
                newName: "Wikidata");

            migrationBuilder.RenameColumn(
                name: "wheelchair",
                table: "places",
                newName: "Wheelchair");

            migrationBuilder.RenameColumn(
                name: "image",
                table: "places",
                newName: "Image");

            migrationBuilder.RenameColumn(
                name: "heritage",
                table: "places",
                newName: "Heritage");

            migrationBuilder.RenameColumn(
                name: "fee",
                table: "places",
                newName: "Fee");

            migrationBuilder.RenameColumn(
                name: "cuisine",
                table: "places",
                newName: "Cuisine");

            migrationBuilder.RenameColumn(
                name: "review_count",
                table: "places",
                newName: "ReviewCount");

            migrationBuilder.RenameColumn(
                name: "price_level",
                table: "places",
                newName: "PriceLevel");

            migrationBuilder.RenameColumn(
                name: "photo_urls",
                table: "places",
                newName: "PhotoUrls");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoUrls",
                table: "places",
                type: "text",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]");

            migrationBuilder.CreateTable(
                name: "stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fallback_place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    added_by = table.Column<string>(type: "text", nullable: false),
                    ai_reasoning = table.Column<string>(type: "text", nullable: true),
                    arrival_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    booking_reference = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency_code = table.Column<string>(type: "text", nullable: false),
                    custom_category = table.Column<string>(type: "text", nullable: true),
                    custom_latitude = table.Column<double>(type: "double precision", nullable: true),
                    custom_longitude = table.Column<double>(type: "double precision", nullable: true),
                    custom_name = table.Column<string>(type: "text", nullable: true),
                    custom_photo_url = table.Column<string>(type: "text", nullable: true),
                    day_number = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_time_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_visited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    order_index = table.Column<double>(type: "double precision", nullable: false),
                    reservation_note = table.Column<string>(type: "text", nullable: true),
                    transport_from_previous = table.Column<string>(type: "text", nullable: true),
                    transport_price = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    travel_time_from_previous = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stops", x => x.id);
                    table.CheckConstraint("ai_reasoning_required", "added_by != 'Ai' OR ai_reasoning IS NOT NULL");
                    table.CheckConstraint("custom_place_requires_category", "custom_name IS NULL OR custom_category IS NOT NULL");
                    table.CheckConstraint("fallback_differs_from_place", "fallback_place_id IS NULL OR fallback_place_id != place_id");
                    table.CheckConstraint("place_or_custom_name", "place_id IS NOT NULL OR custom_name IS NOT NULL");
                    table.CheckConstraint("time_lock_requires_arrival", "is_time_locked = FALSE OR arrival_time IS NOT NULL");
                    table.CheckConstraint("visited_consistency", "is_visited = FALSE OR visited_at IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_stops_places_fallback_place_id",
                        column: x => x.fallback_place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stops_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stops_trips_trip_id",
                        column: x => x.trip_id,
                        principalTable: "trips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stops_fallback_place_id",
                table: "stops",
                column: "fallback_place_id");

            migrationBuilder.CreateIndex(
                name: "IX_stops_place_id",
                table: "stops",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "idx_stops_trip_day_order",
                table: "stops",
                columns: new[] { "trip_id", "day_number", "order_index" });
        }
    }
}
