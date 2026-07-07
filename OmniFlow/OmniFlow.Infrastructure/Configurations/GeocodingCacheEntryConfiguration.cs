using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class GeocodingCacheEntryConfiguration : IEntityTypeConfiguration<GeocodingCacheEntry>
{
    public void Configure(EntityTypeBuilder<GeocodingCacheEntry> builder)
    {
        builder.ToTable("geocoding_cache_entries");

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(40).IsRequired();
        builder.Property(e => e.ForwardKey).HasColumnName("forward_key").HasMaxLength(240);
        builder.Property(e => e.ReverseKey).HasColumnName("reverse_key").HasMaxLength(80);
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(300);
        builder.Property(e => e.City).HasColumnName("city").HasMaxLength(120);
        builder.Property(e => e.Country).HasColumnName("country").HasMaxLength(120);
        builder.Property(e => e.Latitude).HasColumnName("latitude");
        builder.Property(e => e.Longitude).HasColumnName("longitude");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => new { e.Provider, e.ForwardKey })
            .IsUnique()
            .HasFilter("forward_key IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("idx_geocoding_cache_forward");

        builder.HasIndex(e => new { e.Provider, e.ReverseKey })
            .IsUnique()
            .HasFilter("reverse_key IS NOT NULL AND deleted_at IS NULL")
            .HasDatabaseName("idx_geocoding_cache_reverse");
    }
}
