using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class PostUpvoteConfiguration : IEntityTypeConfiguration<PostUpvote>
{
	public void Configure(EntityTypeBuilder<PostUpvote> builder)
	{
		builder.ToTable("post_upvotes");

		builder.HasKey(u => new { u.PostId, u.UserId });

		builder.Property(u => u.PostId).HasColumnName("post_id");
		builder.Property(u => u.UserId).HasColumnName("user_id");
		builder.Property(u => u.CreatedAt).HasColumnName("created_at");

		builder.HasOne<Post>()
			.WithMany()
			.HasForeignKey(u => u.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(u => u.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(u => u.UserId)
			.HasDatabaseName("idx_post_upvotes_user_id");
	}
}
