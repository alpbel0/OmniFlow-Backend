using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
	public void Configure(EntityTypeBuilder<Trip> builder)
	{
		builder.ToTable("trips", t =>
		{
			t.HasCheckConstraint("valid_dates", "end_date >= start_date");
			t.HasCheckConstraint("valid_person_count", "person_count >= 1");
			t.HasCheckConstraint("non_negative_counts",
				"fork_count >= 0 AND upvote_count >= 0 AND view_count >= 0");
		});

		builder.Property(t => t.Id).HasColumnName("id");
		builder.Property(t => t.OwnerId).HasColumnName("owner_id");
		builder.Property(t => t.ForkedFromId).HasColumnName("forked_from_id");
		builder.Property(t => t.Title).HasColumnName("title").IsRequired();
		builder.Property(t => t.Description).HasColumnName("description");
		builder.Property(t => t.CoverPhotoUrl).HasColumnName("cover_photo_url");
		builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>();
		builder.Property(t => t.City).HasColumnName("city").IsRequired();
		builder.Property(t => t.Country).HasColumnName("country").IsRequired();
		builder.Property(t => t.StartDate).HasColumnName("start_date");
		builder.Property(t => t.EndDate).HasColumnName("end_date");
		builder.Property(t => t.PersonCount).HasColumnName("person_count").HasDefaultValue(1);
		builder.Property(t => t.BudgetTier).HasColumnName("budget_tier").HasConversion<string>();
		builder.Property(t => t.TravelStyle).HasColumnName("travel_style").HasConversion<string>();
		builder.Property(t => t.UserBudget).HasColumnName("user_budget");
		builder.Property(t => t.EstimatedCost).HasColumnName("estimated_cost");
		builder.Property(t => t.ForkCount).HasColumnName("fork_count").HasDefaultValue(0);
		builder.Property(t => t.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
		builder.Property(t => t.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
		builder.Property(t => t.PopularityScore).HasColumnName("popularity_score").HasDefaultValue(0m);
		builder.Property(t => t.Tags).HasColumnName("tags").HasColumnType("text[]");
		builder.Property(t => t.CreatedAt).HasColumnName("created_at");
		builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
		builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

		builder.HasOne(t => t.Owner)
			.WithMany(u => u.Trips)
			.HasForeignKey(t => t.OwnerId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne<Trip>()
			.WithMany()
			.HasForeignKey(t => t.ForkedFromId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(t => t.Tags)
			.HasMethod("gin")
			.HasDatabaseName("idx_trips_tags_gin");

		builder.HasIndex(t => t.Status)
			.HasFilter("status = 'Published' AND deleted_at IS NULL")
			.HasDatabaseName("idx_trips_explore");
	}
}
