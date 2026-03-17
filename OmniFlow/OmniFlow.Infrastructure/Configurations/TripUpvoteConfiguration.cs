using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TripUpvoteConfiguration : IEntityTypeConfiguration<TripUpvote>
{
	public void Configure(EntityTypeBuilder<TripUpvote> builder)
	{
		builder.ToTable("trip_upvotes");

		builder.HasKey(u => new { u.TripId, u.UserId });

		builder.Property(u => u.TripId).HasColumnName("trip_id");
		builder.Property(u => u.UserId).HasColumnName("user_id");
		builder.Property(u => u.CreatedAt).HasColumnName("created_at");

		builder.HasOne<Trip>()
			.WithMany()
			.HasForeignKey(u => u.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(u => u.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(u => u.UserId)
			.HasDatabaseName("idx_trip_upvotes_user_id");
	}
}
