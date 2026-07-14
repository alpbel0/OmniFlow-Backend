using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations;

public partial class AddCurrencyTimezoneFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("preferred_currency_code", "users", type: "character varying(3)", maxLength: 3, nullable: true);
        migrationBuilder.AddColumn<string>("base_currency_code", "trips", type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD");
        migrationBuilder.AddColumn<string>("timezone", "trip_destinations", type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddCheckConstraint(
            name: "trips_supported_base_currency",
            table: "trips",
            sql: "base_currency_code IN ('TRY', 'USD', 'EUR')");
        migrationBuilder.AddCheckConstraint(
            name: "users_supported_preferred_currency",
            table: "users",
            sql: "preferred_currency_code IS NULL OR preferred_currency_code IN ('TRY', 'USD', 'EUR')");

        migrationBuilder.CreateTable(
            name: "exchange_rate_snapshots",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                quote_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                rate_date = table.Column<DateOnly>(type: "date", nullable: false),
                rate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                fetched_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_exchange_rate_snapshots", x => x.id);
                table.CheckConstraint("exchange_rate_currency_codes", "base_currency IN ('TRY', 'USD', 'EUR') AND quote_currency IN ('TRY', 'USD', 'EUR')");
                table.CheckConstraint("exchange_rate_positive", "rate > 0");
            });
        migrationBuilder.CreateIndex(
            name: "ux_exchange_rate_pair_date_provider",
            table: "exchange_rate_snapshots",
            columns: new[] { "base_currency", "quote_currency", "rate_date", "provider" },
            unique: true,
            filter: "deleted_at IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("exchange_rate_snapshots");
        migrationBuilder.DropCheckConstraint("trips_supported_base_currency", "trips");
        migrationBuilder.DropCheckConstraint("users_supported_preferred_currency", "users");
        migrationBuilder.DropColumn("preferred_currency_code", "users");
        migrationBuilder.DropColumn("base_currency_code", "trips");
        migrationBuilder.DropColumn("timezone", "trip_destinations");
    }
}
