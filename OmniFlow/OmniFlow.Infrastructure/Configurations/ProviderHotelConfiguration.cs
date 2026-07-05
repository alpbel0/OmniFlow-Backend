using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class ProviderHotelConfiguration : IEntityTypeConfiguration<ProviderHotel>
{
	public void Configure(EntityTypeBuilder<ProviderHotel> builder)
	{
		builder.ToTable("provider_hotels", t =>
		{
			t.HasCheckConstraint("valid_provider_hotel_stars",
				"stars IS NULL OR (stars >= 1 AND stars <= 5)");
			t.HasCheckConstraint("valid_provider_hotel_rating",
				"rating IS NULL OR (rating >= 0 AND rating <= 10)");
			t.HasCheckConstraint("valid_provider_hotel_review_count",
				"review_count IS NULL OR review_count >= 0");
			t.HasCheckConstraint("valid_provider_hotel_price",
				"price_per_night >= 0");
			t.HasCheckConstraint("valid_provider_hotel_currency",
				"char_length(currency_code) = 3");
		});

		builder.Property(h => h.Id).HasColumnName("id");
		builder.Property(h => h.HotelName).HasColumnName("hotel_name").HasMaxLength(200).IsRequired();
		builder.Property(h => h.City).HasColumnName("city").HasMaxLength(100).IsRequired();
		builder.Property(h => h.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
		builder.Property(h => h.Latitude).HasColumnName("latitude");
		builder.Property(h => h.Longitude).HasColumnName("longitude");
		builder.Property(h => h.Stars).HasColumnName("stars");
		builder.Property(h => h.Rating).HasColumnName("rating");
		builder.Property(h => h.ReviewCount).HasColumnName("review_count");
		builder.Property(h => h.ValidDate).HasColumnName("valid_date").HasColumnType("date");
		builder.Property(h => h.PricePerNight).HasColumnName("price_per_night").HasPrecision(18, 2);
		builder.Property(h => h.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(h => h.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(1000);
		builder.Property(h => h.ProviderName).HasColumnName("provider_name").HasMaxLength(50).IsRequired();
		builder.Property(h => h.ProviderUrl).HasColumnName("provider_url").HasMaxLength(1000);
		builder.Property(h => h.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
		builder.Property(h => h.LastUpdatedAt).HasColumnName("last_updated_at")
			.HasColumnType("timestamp without time zone")
			.HasDefaultValueSql("(now() at time zone 'utc')");
		builder.Property(h => h.IsLiveData).HasColumnName("is_live_data").HasDefaultValue(false);
		builder.Property(h => h.DataSnapshotDate).HasColumnName("data_snapshot_date")
			.HasColumnType("date")
			.HasDefaultValueSql("CURRENT_DATE");

		builder.HasIndex(h => new { h.City, h.Country, h.ValidDate })
			.HasDatabaseName("idx_provider_hotels_city_country_date");

		builder.HasIndex(h => h.IsAvailable)
			.HasDatabaseName("idx_provider_hotels_is_available");
	}
}
