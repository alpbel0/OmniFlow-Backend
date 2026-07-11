using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportFromToCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "transport_from_latitude",
                table: "timeline_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "transport_from_longitude",
                table: "timeline_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "transport_to_latitude",
                table: "timeline_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "transport_to_longitude",
                table: "timeline_entries",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "transport_from_latitude",
                table: "timeline_entries");

            migrationBuilder.DropColumn(
                name: "transport_from_longitude",
                table: "timeline_entries");

            migrationBuilder.DropColumn(
                name: "transport_to_latitude",
                table: "timeline_entries");

            migrationBuilder.DropColumn(
                name: "transport_to_longitude",
                table: "timeline_entries");
        }
    }
}
