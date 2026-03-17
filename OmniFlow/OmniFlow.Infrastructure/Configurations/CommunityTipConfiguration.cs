using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class CommunityTipConfiguration : IEntityTypeConfiguration<CommunityTip>
{
	public void Configure(EntityTypeBuilder<CommunityTip> builder)
	{
		builder.ToTable("community_tips", t =>
		{
			t.HasCheckConstraint("valid_content", "length(content) > 0");
		});

		builder.Property(t => t.Id).HasColumnName("id");
		builder.Property(t => t.TripId).HasColumnName("trip_id");
		builder.Property(t => t.UserId).HasColumnName("user_id");
		builder.Property(t => t.PlaceId).HasColumnName("place_id");
		builder.Property(t => t.Content).HasColumnName("content").IsRequired();
		builder.Property(t => t.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
		builder.Property(t => t.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
		builder.Property(t => t.CreatedAt).HasColumnName("created_at");
		builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
		builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

		builder.HasOne(t => t.Trip)
			.WithMany()
			.HasForeignKey(t => t.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(t => t.User)
			.WithMany()
			.HasForeignKey(t => t.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(t => t.Place)
			.WithMany()
			.HasForeignKey(t => t.PlaceId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(t => new { t.TripId, t.UserId })
			.HasFilter("deleted_at IS NULL AND is_visible = TRUE")
			.HasDatabaseName("idx_community_tips_visible");
	}
}
