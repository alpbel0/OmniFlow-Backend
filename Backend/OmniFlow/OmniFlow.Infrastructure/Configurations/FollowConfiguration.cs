using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
	public void Configure(EntityTypeBuilder<Follow> builder)
	{
		builder.ToTable("follows", t =>
		{
			t.HasCheckConstraint("no_self_follow", "follower_id != following_id");
		});

		builder.HasKey(f => new { f.FollowerId, f.FollowingId });

		builder.Property(f => f.FollowerId).HasColumnName("follower_id");
		builder.Property(f => f.FollowingId).HasColumnName("following_id");
		builder.Property(f => f.CreatedAt).HasColumnName("created_at");

		builder.HasOne(f => f.Follower)
			.WithMany(u => u.Following)
			.HasForeignKey(f => f.FollowerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(f => f.Following)
			.WithMany(u => u.Followers)
			.HasForeignKey(f => f.FollowingId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(f => f.FollowingId)
			.HasDatabaseName("idx_follows_following_id");
	}
}
