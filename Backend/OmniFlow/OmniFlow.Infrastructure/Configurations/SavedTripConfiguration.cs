using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class SavedTripConfiguration : IEntityTypeConfiguration<SavedTrip>
{
	public void Configure(EntityTypeBuilder<SavedTrip> builder)
	{
		builder.ToTable("saved_trips");

		builder.HasKey(s => new { s.UserId, s.TripId });

		builder.Property(s => s.UserId).HasColumnName("user_id");
		builder.Property(s => s.TripId).HasColumnName("trip_id");
		builder.Property(s => s.CreatedAt).HasColumnName("created_at");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(s => s.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<Trip>()
			.WithMany()
			.HasForeignKey(s => s.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(s => s.TripId)
			.HasDatabaseName("idx_saved_trips_trip_id");
	}
}
