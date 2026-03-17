using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("users", t =>
		{
			t.HasCheckConstraint("non_negative_follow_counts",
				"followers_count >= 0 AND following_count >= 0");
			t.HasCheckConstraint("username_format",
				"username ~ '^[a-zA-Z0-9_]{3,30}$'");
		});

		builder.Property(u => u.Id).HasColumnName("id");
		builder.Property(u => u.Username).HasColumnName("username").HasColumnType("citext").IsRequired();
		builder.Property(u => u.Email).HasColumnName("email").HasColumnType("citext").IsRequired();
		builder.Property(u => u.Bio).HasColumnName("bio");
		builder.Property(u => u.ProfilePhotoUrl).HasColumnName("profile_photo_url");
		builder.Property(u => u.KarmaScore).HasColumnName("karma_score").HasDefaultValue(0);
		builder.Property(u => u.FollowersCount).HasColumnName("followers_count").HasDefaultValue(0);
		builder.Property(u => u.FollowingCount).HasColumnName("following_count").HasDefaultValue(0);
		builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>();
		builder.Property(u => u.IsVerified).HasColumnName("is_verified").HasDefaultValue(false);
		builder.Property(u => u.IsSuspended).HasColumnName("is_suspended").HasDefaultValue(false);
		builder.Property(u => u.CreatedAt).HasColumnName("created_at");
		builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
		builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");

		builder.HasIndex(u => u.Username)
			.HasFilter("deleted_at IS NULL")
			.IsUnique()
			.HasDatabaseName("idx_users_username_unique");

		builder.HasIndex(u => u.Email)
			.HasFilter("deleted_at IS NULL")
			.IsUnique()
			.HasDatabaseName("idx_users_email_unique");
	}
}
