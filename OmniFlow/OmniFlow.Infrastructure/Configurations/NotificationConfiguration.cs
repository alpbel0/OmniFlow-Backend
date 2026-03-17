using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder.ToTable("notifications", t =>
		{
			t.HasCheckConstraint("follow_has_no_target",
				"notification_type != 'Follow' OR (target_id IS NULL AND target_type IS NULL)");
			t.HasCheckConstraint("valid_notification_target_type",
				"(notification_type = 'Follow') OR " +
				"(notification_type IN ('PostUpvote', 'Comment', 'Mention') AND target_type = 'Post') OR " +
				"(notification_type = 'CommentUpvote' AND target_type = 'Comment') OR " +
				"(notification_type = 'TipUpvote' AND target_type = 'Tip') OR " +
				"(notification_type IN ('TripUpvote', 'Fork') AND target_type = 'Trip')");
			t.HasCheckConstraint("read_consistency",
				"is_read = FALSE OR read_at IS NOT NULL");
		});

		builder.Property(n => n.Id).HasColumnName("id");
		builder.Property(n => n.UserId).HasColumnName("user_id");
		builder.Property(n => n.ActorId).HasColumnName("actor_id");
		builder.Property(n => n.NotificationType).HasColumnName("notification_type").HasConversion<string>();
		builder.Property(n => n.TargetId).HasColumnName("target_id");
		builder.Property(n => n.TargetType).HasColumnName("target_type").HasConversion<string>();
		builder.Property(n => n.IsRead).HasColumnName("is_read").HasDefaultValue(false);
		builder.Property(n => n.ReadAt).HasColumnName("read_at");
		builder.Property(n => n.CreatedAt).HasColumnName("created_at");

		builder.HasOne(n => n.User)
			.WithMany()
			.HasForeignKey(n => n.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(n => n.Actor)
			.WithMany()
			.HasForeignKey(n => n.ActorId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(n => n.UserId)
			.HasFilter("is_read = FALSE")
			.HasDatabaseName("idx_notifications_unread");
	}
}
