using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using System.Text.Json;

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
		builder.Property(u => u.Location).HasColumnName("location").HasMaxLength(120);

		var travelStyleConverter = new ValueConverter<List<TravelStyle>, string>(
			v => SerializeTravelStyles(v),
			v => DeserializeTravelStyles(v));
		var travelStyleComparer = new ValueComparer<List<TravelStyle>>(
			(a, b) => a != null && b != null && a.SequenceEqual(b),
			v => v == null ? 0 : v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
			v => v == null ? new List<TravelStyle>() : v.ToList());
		builder.Property(u => u.TravelStyles)
			.HasColumnName("travel_styles")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasConversion(travelStyleConverter, travelStyleComparer)
			.IsRequired();

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

	private static string SerializeTravelStyles(List<TravelStyle>? travelStyles)
	{
		var styleNames = travelStyles?.Select(style => style.ToString()).ToArray()
			?? Array.Empty<string>();

		return JsonSerializer.Serialize(styleNames);
	}

	private static List<TravelStyle> DeserializeTravelStyles(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return new List<TravelStyle>();
		}

		var styleNames = JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
		return styleNames.Select(style => Enum.Parse<TravelStyle>(style)).ToList();
	}
}
