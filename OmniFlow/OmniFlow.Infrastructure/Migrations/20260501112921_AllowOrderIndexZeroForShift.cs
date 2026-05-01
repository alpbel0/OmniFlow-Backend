using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowOrderIndexZeroForShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "valid_order_index",
                table: "trip_destinations");

            migrationBuilder.AddCheckConstraint(
                name: "valid_order_index",
                table: "trip_destinations",
                sql: "order_index BETWEEN 0 AND 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "valid_order_index",
                table: "trip_destinations");

            migrationBuilder.AddCheckConstraint(
                name: "valid_order_index",
                table: "trip_destinations",
                sql: "order_index BETWEEN 1 AND 10");
        }
    }
}
