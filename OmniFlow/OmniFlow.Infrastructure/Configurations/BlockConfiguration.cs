using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
	public void Configure(EntityTypeBuilder<Block> builder)
	{
		builder.ToTable("blocks", t =>
		{
			t.HasCheckConstraint("no_self_block", "blocker_id != blocked_user_id");
		});

		builder.HasKey(block => new { block.BlockerId, block.BlockedUserId });

		builder.Property(block => block.BlockerId).HasColumnName("blocker_id");
		builder.Property(block => block.BlockedUserId).HasColumnName("blocked_user_id");
		builder.Property(block => block.CreatedAt).HasColumnName("created_at");

		builder.HasOne(block => block.Blocker)
			.WithMany(user => user.BlockedUsers)
			.HasForeignKey(block => block.BlockerId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(block => block.BlockedUser)
			.WithMany(user => user.BlockedByUsers)
			.HasForeignKey(block => block.BlockedUserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(block => block.BlockedUserId)
			.HasDatabaseName("idx_blocks_blocked_user_id");
	}
}