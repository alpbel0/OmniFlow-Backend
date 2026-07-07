using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TripRouteCacheConfiguration : IEntityTypeConfiguration<TripRouteCache>
{
    public void Configure(EntityTypeBuilder<TripRouteCache> builder)
    {
        builder.ToTable("trip_route_caches");

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TripId).HasColumnName("trip_id").IsRequired();
        builder.Property(e => e.RouteSignature).HasColumnName("route_signature").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ResponseJson).HasColumnName("response_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(40).IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.TripId)
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("idx_trip_route_caches_trip_id_unique");

        builder.HasOne(e => e.Trip)
            .WithMany()
            .HasForeignKey(e => e.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
