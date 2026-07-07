using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TripDestinationConfiguration : IEntityTypeConfiguration<TripDestination>
{
	public void Configure(EntityTypeBuilder<TripDestination> builder)
	{
		builder.ToTable("trip_destinations", t =>
		{
			t.HasCheckConstraint("valid_order_index", "order_index BETWEEN 0 AND 10");
			t.HasCheckConstraint("valid_dates", "departure_date >= arrival_date");
			t.HasCheckConstraint("valid_night_count", "night_count >= 0");
		});

		builder.Property(d => d.Id).HasColumnName("id");
		builder.Property(d => d.TripId).HasColumnName("trip_id").IsRequired();
		builder.Property(d => d.City).HasColumnName("city").IsRequired();
		builder.Property(d => d.Country).HasColumnName("country").IsRequired();
		builder.Property(d => d.Latitude).HasColumnName("latitude");
		builder.Property(d => d.Longitude).HasColumnName("longitude");

		// Backing fields for EF Core materialization to avoid premature RecalculateNightCount calls
		builder.Property(d => d.ArrivalDate)
			.HasColumnName("arrival_date")
			.HasField("_arrivalDate")
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.Property(d => d.DepartureDate)
			.HasColumnName("departure_date")
			.HasField("_departureDate")
			.UsePropertyAccessMode(PropertyAccessMode.Field);

		builder.Property(d => d.OrderIndex).HasColumnName("order_index").IsRequired();
		builder.Property(d => d.NightCount).HasColumnName("night_count").IsRequired();
		builder.Property(d => d.CreatedAt).HasColumnName("created_at");
		builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
		builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");

		builder.HasOne(d => d.Trip)
			.WithMany(t => t.Destinations)
			.HasForeignKey(d => d.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(d => d.TimelineEntries)
			.WithOne(e => e.Destination)
			.HasForeignKey(e => e.DestinationId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(d => new { d.TripId, d.OrderIndex })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("idx_trip_destinations_trip_order");

		builder.HasIndex(d => d.City)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("idx_trip_destinations_city");
	}
}
