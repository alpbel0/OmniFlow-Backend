using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    public partial class MakeTripDestinationOrderIndexDeferrableWithSoftDeleteFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_trip_destinations_trip_order",
                table: "trip_destinations");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX idx_trip_destinations_trip_order 
                  ON trip_destinations (trip_id, order_index) 
                  DEFERRABLE INITIALLY DEFERRED
                  WHERE deleted_at IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_trip_destinations_trip_order",
                table: "trip_destinations");

            migrationBuilder.CreateIndex(
                name: "idx_trip_destinations_trip_order",
                table: "trip_destinations",
                columns: new[] { "trip_id", "order_index" },
                unique: true);
        }
    }
}