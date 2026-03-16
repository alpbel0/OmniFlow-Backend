using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
	public void Configure(EntityTypeBuilder<Post> builder)
	{
		builder.ToTable("posts", t =>
		{
			t.HasCheckConstraint("route_requires_trip",
				"post_type != 'Route' OR trip_id IS NOT NULL");
			t.HasCheckConstraint("content_or_photo",
				"content IS NOT NULL OR cardinality(photos) > 0");
			t.HasCheckConstraint("non_negative_counts",
				"upvote_count >= 0 AND comment_count >= 0");
		});

		builder.Property(p => p.Id).HasColumnName("id");
		builder.Property(p => p.UserId).HasColumnName("user_id");
		builder.Property(p => p.TripId).HasColumnName("trip_id");
		builder.Property(p => p.PlaceId).HasColumnName("place_id");
		builder.Property(p => p.PostType).HasColumnName("post_type").HasConversion<string>();
		builder.Property(p => p.Content).HasColumnName("content");
		builder.Property(p => p.Photos).HasColumnName("photos").HasColumnType("text[]");
		builder.Property(p => p.Tags).HasColumnName("tags").HasColumnType("text[]");
		builder.Property(p => p.AiTags).HasColumnName("ai_tags").HasColumnType("text[]");
		builder.Property(p => p.LocationLatitude).HasColumnName("location_latitude");
		builder.Property(p => p.LocationLongitude).HasColumnName("location_longitude");
		builder.Property(p => p.City).HasColumnName("city");
		builder.Property(p => p.Country).HasColumnName("country");
		builder.Property(p => p.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
		builder.Property(p => p.CommentCount).HasColumnName("comment_count").HasDefaultValue(0);
		builder.Property(p => p.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
		builder.Property(p => p.CreatedAt).HasColumnName("created_at");
		builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
		builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

		builder.HasOne(p => p.User)
			.WithMany(u => u.Posts)
			.HasForeignKey(p => p.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(p => p.Trip)
			.WithMany()
			.HasForeignKey(p => p.TripId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(p => p.Place)
			.WithMany()
			.HasForeignKey(p => p.PlaceId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(p => p.UserId)
			.HasFilter("deleted_at IS NULL AND is_visible = TRUE")
			.HasDatabaseName("idx_posts_visible");

		builder.HasIndex(p => p.Tags)
			.HasMethod("gin")
			.HasDatabaseName("idx_posts_tags_gin");

		builder.HasIndex(p => p.AiTags)
			.HasMethod("gin")
			.HasDatabaseName("idx_posts_ai_tags_gin");
	}
}
