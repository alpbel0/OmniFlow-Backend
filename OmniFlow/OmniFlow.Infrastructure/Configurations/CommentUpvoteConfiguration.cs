using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class CommentUpvoteConfiguration : IEntityTypeConfiguration<CommentUpvote>
{
	public void Configure(EntityTypeBuilder<CommentUpvote> builder)
	{
		builder.ToTable("comment_upvotes");

		builder.HasKey(u => new { u.CommentId, u.UserId });

		builder.Property(u => u.CommentId).HasColumnName("comment_id");
		builder.Property(u => u.UserId).HasColumnName("user_id");
		builder.Property(u => u.CreatedAt).HasColumnName("created_at");

		builder.HasOne<Comment>()
			.WithMany()
			.HasForeignKey(u => u.CommentId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(u => u.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(u => u.UserId)
			.HasDatabaseName("idx_comment_upvotes_user_id");
	}
}
