using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OmniFlow.Infrastructure.Contexts;

#nullable disable

namespace OmniFlow.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260714120000_AddNearbyPlaceSpatialIndex")]
public sealed class AddNearbyPlaceSpatialIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS idx_places_active_geography_gist
            ON places
            USING GIST ((ST_SetSRID(ST_MakePoint(longitude, latitude), 4326)::geography))
            WHERE is_active = TRUE AND category NOT IN ('Hotel', 'Transport');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS idx_places_active_geography_gist;");
    }
}
