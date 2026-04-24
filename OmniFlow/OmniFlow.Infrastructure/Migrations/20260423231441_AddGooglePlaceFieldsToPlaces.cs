using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGooglePlaceFieldsToPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrls",
                table: "places",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceLevel",
                table: "places",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "places",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrls",
                table: "places");

            migrationBuilder.DropColumn(
                name: "PriceLevel",
                table: "places");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "places");
        }
    }
}
