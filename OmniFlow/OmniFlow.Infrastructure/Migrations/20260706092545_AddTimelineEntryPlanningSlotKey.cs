using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimelineEntryPlanningSlotKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "planning_slot_key",
                table: "timeline_entries",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_timeline_entries_trip_planning_slot_active",
                table: "timeline_entries",
                columns: new[] { "trip_id", "planning_slot_key" },
                unique: true,
                filter: "planning_slot_key IS NOT NULL AND deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_timeline_entries_trip_planning_slot_active",
                table: "timeline_entries");

            migrationBuilder.DropColumn(
                name: "planning_slot_key",
                table: "timeline_entries");
        }
    }
}
