using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
	public void Configure(EntityTypeBuilder<RefreshToken> builder)
	{
		builder.ToTable("refresh_tokens", t =>
		{
			t.HasCheckConstraint("valid_expiry", "expires_at > created_at");
		});

		builder.Property(r => r.Id).HasColumnName("id");
		builder.Property(r => r.UserId).HasColumnName("user_id");
		builder.Property(r => r.TokenHash).HasColumnName("token_hash").IsRequired();
		builder.Property(r => r.ExpiresAt).HasColumnName("expires_at");
		builder.Property(r => r.RevokedAt).HasColumnName("revoked_at");
		builder.Property(r => r.DeviceFingerprint).HasColumnName("device_fingerprint");
		builder.Property(r => r.CreatedAt).HasColumnName("created_at");

		builder.HasOne<User>()
			.WithMany()
			.HasForeignKey(r => r.UserId)
			.OnDelete(DeleteBehavior.Cascade);

		// Active token must be unique
		builder.HasIndex(r => r.TokenHash)
			.HasFilter("revoked_at IS NULL")
			.IsUnique()
			.HasDatabaseName("idx_refresh_tokens_hash_active");

		// Fast lookup of active tokens by user
		builder.HasIndex(r => r.UserId)
			.HasFilter("revoked_at IS NULL")
			.HasDatabaseName("idx_refresh_tokens_user_active");
	}
}
