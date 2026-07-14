using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations;

public partial class AddPlaceVisitLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "place_visit_logs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                trip_id = table.Column<Guid>(type: "uuid", nullable: false),
                trip_destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                timeline_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                place_id = table.Column<Guid>(type: "uuid", nullable: true),
                actual_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                rating = table.Column<int>(type: "integer", nullable: true),
                note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                converted_actual_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                exchange_rate = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                rate_requested_date = table.Column<DateOnly>(type: "date", nullable: true),
                exchange_rate_date = table.Column<DateOnly>(type: "date", nullable: true),
                base_currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                conversion_status = table.Column<string>(type: "text", nullable: false),
                conversion_attempt_count = table.Column<int>(type: "integer", nullable: false),
                last_conversion_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_place_visit_logs", x => x.id);
                table.CheckConstraint("visit_log_cost_non_negative", "actual_cost IS NULL OR actual_cost >= 0");
                table.CheckConstraint("visit_log_currency_codes", "currency_code IN ('TRY', 'USD', 'EUR') AND base_currency_code IN ('TRY', 'USD', 'EUR')");
                table.CheckConstraint("visit_log_rating_range", "rating IS NULL OR rating BETWEEN 1 AND 5");
                table.CheckConstraint("visit_log_target_xor", "(timeline_entry_id IS NOT NULL) <> (place_id IS NOT NULL)");
                table.ForeignKey("FK_place_visit_logs_places_place_id", x => x.place_id, "places", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_place_visit_logs_timeline_entries_timeline_entry_id", x => x.timeline_entry_id, "timeline_entries", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_place_visit_logs_trip_destinations_trip_destination_id", x => x.trip_destination_id, "trip_destinations", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_place_visit_logs_trips_trip_id", x => x.trip_id, "trips", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_place_visit_logs_users_user_id", x => x.user_id, "users", "id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_place_visit_logs_place_id", "place_visit_logs", "place_id");
        migrationBuilder.CreateIndex("IX_place_visit_logs_user_id", "place_visit_logs", "user_id");
        migrationBuilder.CreateIndex("ix_visit_logs_destination_visited_at", "place_visit_logs", new[] { "trip_destination_id", "visited_at" });
        migrationBuilder.CreateIndex("ix_visit_logs_pending_conversion", "place_visit_logs", new[] { "conversion_status", "last_conversion_attempt_at_utc" });
        migrationBuilder.CreateIndex("ix_visit_logs_trip_user", "place_visit_logs", new[] { "trip_id", "user_id" });
        migrationBuilder.CreateIndex("ux_visit_log_timeline_active", "place_visit_logs", "timeline_entry_id", unique: true, filter: "timeline_entry_id IS NOT NULL AND deleted_at IS NULL");
        migrationBuilder.Sql("""
            CREATE FUNCTION validate_place_visit_log_relationships() RETURNS trigger AS $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM trip_destinations d WHERE d.id = NEW.trip_destination_id AND d.trip_id = NEW.trip_id) THEN
                    RAISE EXCEPTION 'Visit log destination must belong to the trip';
                END IF;
                IF NOT EXISTS (SELECT 1 FROM trips t WHERE t.id = NEW.trip_id AND t.owner_id = NEW.user_id) THEN
                    RAISE EXCEPTION 'Visit log user must own the trip';
                END IF;
                IF NEW.timeline_entry_id IS NOT NULL AND NOT EXISTS (
                    SELECT 1 FROM timeline_entries e
                    WHERE e.id = NEW.timeline_entry_id AND e.trip_id = NEW.trip_id AND e.destination_id = NEW.trip_destination_id
                ) THEN
                    RAISE EXCEPTION 'Visit log timeline entry must belong to the trip destination';
                END IF;
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER trg_validate_place_visit_log_relationships
            BEFORE INSERT OR UPDATE OF trip_id, trip_destination_id, user_id, timeline_entry_id
            ON place_visit_logs FOR EACH ROW EXECUTE FUNCTION validate_place_visit_log_relationships();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_validate_place_visit_log_relationships ON place_visit_logs; DROP FUNCTION IF EXISTS validate_place_visit_log_relationships();");
        migrationBuilder.DropTable("place_visit_logs");
    }
}
