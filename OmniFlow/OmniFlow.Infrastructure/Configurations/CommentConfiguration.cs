using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
	public void Configure(EntityTypeBuilder<Comment> builder)
	{
		builder.ToTable("comments", t =>
		{
			t.HasCheckConstraint("valid_content", "length(content) > 0");
		});

		builder.Property(c => c.Id).HasColumnName("id");
		builder.Property(c => c.PostId).HasColumnName("post_id");
		builder.Property(c => c.UserId).HasColumnName("user_id");
		builder.Property(c => c.ParentCommentId).HasColumnName("parent_comment_id");
		builder.Property(c => c.Content).HasColumnName("content").IsRequired();
		builder.Property(c => c.Mentions).HasColumnName("mentions").HasColumnType("text[]");
		builder.Property(c => c.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
		builder.Property(c => c.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
		builder.Property(c => c.CreatedAt).HasColumnName("created_at");
		builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");
		builder.Property(c => c.DeletedAt).HasColumnName("deleted_at");

		// Alternate key needed for composite FK reference
		builder.HasAlternateKey(c => new { c.Id, c.PostId })
			.HasName("uq_comments_id_post_id");

		builder.HasOne(c => c.Post)
			.WithMany(p => p.Comments)
			.HasForeignKey(c => c.PostId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(c => c.User)
			.WithMany()
			.HasForeignKey(c => c.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		// Composite FK: (parent_comment_id, post_id) → comments(id, post_id) — cross-post protection
		builder.HasOne(c => c.ParentComment)
			.WithMany(c => c.Replies)
			.HasForeignKey(c => new { c.ParentCommentId, c.PostId })
			.HasPrincipalKey(c => new { c.Id, c.PostId })
			.IsRequired(false)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(c => c.Mentions)
			.HasMethod("gin")
			.HasDatabaseName("idx_comments_mentions_gin");
	}
}
