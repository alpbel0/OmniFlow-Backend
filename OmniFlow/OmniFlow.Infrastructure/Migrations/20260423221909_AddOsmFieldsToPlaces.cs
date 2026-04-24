using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOsmFieldsToPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cuisine",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fee",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Heritage",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Wheelchair",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Wikidata",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Wikipedia",
                table: "places",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cuisine",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Heritage",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Wheelchair",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Wikidata",
                table: "places");

            migrationBuilder.DropColumn(
                name: "Wikipedia",
                table: "places");
        }
    }
}
