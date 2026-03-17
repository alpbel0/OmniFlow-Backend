using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class TipUpvoteConfiguration : IEntityTypeConfiguration<TipUpvote>
{
	public void Configure(EntityTypeBuilder<TipUpvote> builder)
	{
		builder.ToTable("tip_upvotes");

		builder.HasKey(u => new { u.TipId, u.UserId });

		builder.Property(u => u.TipId).HasColumnName("tip_id");
		builder.Property(u => u.UserId).HasColumnName("user_id");
		builder.Property(u => u.CreatedAt).HasColumnName("created_at");

		builder.HasOne<CommunityTip>()
			.WithMany()
			.HasForeignKey(u => u.TipId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(u => u.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(u => u.UserId)
			.HasDatabaseName("idx_tip_upvotes_user_id");
	}
}
