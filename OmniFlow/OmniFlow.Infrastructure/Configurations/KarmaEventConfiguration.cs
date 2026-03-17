using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class KarmaEventConfiguration : IEntityTypeConfiguration<KarmaEvent>
{
	public void Configure(EntityTypeBuilder<KarmaEvent> builder)
	{
		builder.ToTable("karma_events", t =>
		{
			t.HasCheckConstraint("valid_event_source_type",
				"(event_type IN ('TripPublished', 'TripForked', 'TripUpvoted') AND source_type = 'Trip') OR " +
				"(event_type = 'PostUpvoted' AND source_type = 'Post') OR " +
				"(event_type = 'TipUpvoted' AND source_type = 'Tip')");
			t.HasCheckConstraint("source_consistency",
				"source_type IS NULL OR source_id IS NOT NULL");
			t.HasCheckConstraint("valid_points", "points != 0");
		});

		builder.Property(k => k.Id).HasColumnName("id");
		builder.Property(k => k.UserId).HasColumnName("user_id");
		builder.Property(k => k.ActorId).HasColumnName("actor_id");
		builder.Property(k => k.EventType).HasColumnName("event_type").HasConversion<string>();
		builder.Property(k => k.Points).HasColumnName("points");
		builder.Property(k => k.SourceId).HasColumnName("source_id");
		builder.Property(k => k.SourceType).HasColumnName("source_type").HasConversion<string>();
		builder.Property(k => k.CreatedAt).HasColumnName("created_at");

		builder.HasOne(k => k.User)
			.WithMany()
			.HasForeignKey(k => k.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(k => k.Actor)
			.WithMany()
			.HasForeignKey(k => k.ActorId)
			.OnDelete(DeleteBehavior.SetNull);

		// Anti-farming: one publish event per trip per user
		builder.HasIndex(k => new { k.UserId, k.SourceId, k.EventType })
			.HasFilter("event_type = 'TripPublished'")
			.IsUnique()
			.HasDatabaseName("idx_karma_publish_unique");

		// Anti-farming: one interaction event per (user, source, type, actor)
		builder.HasIndex(k => new { k.UserId, k.SourceId, k.EventType, k.ActorId })
			.HasFilter("event_type != 'TripPublished'")
			.IsUnique()
			.HasDatabaseName("idx_karma_interaction_unique");
	}
}
