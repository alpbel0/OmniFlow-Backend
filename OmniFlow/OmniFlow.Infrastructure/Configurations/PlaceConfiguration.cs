using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
	public void Configure(EntityTypeBuilder<Place> builder)
	{
		builder.ToTable("places", t =>
		{
			t.HasCheckConstraint("valid_rating",
				"rating IS NULL OR (rating >= 1 AND rating <= 5)");
			t.HasCheckConstraint("free_has_zero_price",
				"NOT is_free OR estimated_price = 0");
			t.HasCheckConstraint("valid_best_months",
				"best_months IS NULL OR best_months <@ ARRAY[1,2,3,4,5,6,7,8,9,10,11,12]");
		});

		builder.Property(p => p.Id).HasColumnName("id");
		builder.Property(p => p.Name).HasColumnName("name").IsRequired();
		builder.Property(p => p.Description).HasColumnName("description");
		builder.Property(p => p.Category).HasColumnName("category").HasConversion<string>();
		builder.Property(p => p.PhotoUrl).HasColumnName("photo_url");
		builder.Property(p => p.Phone).HasColumnName("phone");
		builder.Property(p => p.WebsiteUrl).HasColumnName("website_url");
		builder.Property(p => p.Latitude).HasColumnName("latitude");
		builder.Property(p => p.Longitude).HasColumnName("longitude");
		builder.Property(p => p.Address).HasColumnName("address");
		builder.Property(p => p.City).HasColumnName("city").IsRequired();
		builder.Property(p => p.Country).HasColumnName("country").IsRequired();
		builder.Property(p => p.Timezone).HasColumnName("timezone");
		builder.Property(p => p.GooglePlaceId).HasColumnName("google_place_id");
		builder.Property(p => p.EstimatedPrice).HasColumnName("estimated_price").HasDefaultValue(0m);
		builder.Property(p => p.CurrencyCode).HasColumnName("currency_code").HasDefaultValue("USD");
		builder.Property(p => p.IsFree).HasColumnName("is_free");
		builder.Property(p => p.DurationMinutes).HasColumnName("duration_minutes");
		builder.Property(p => p.Rating).HasColumnName("rating");
		builder.Property(p => p.OpeningHours).HasColumnName("opening_hours").HasColumnType("jsonb");
		builder.Property(p => p.BestMonths).HasColumnName("best_months").HasColumnType("integer[]");
		builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);

		// Eksik mapping'ler
		builder.Property(p => p.PriceLevel).HasColumnName("price_level");
		builder.Property(p => p.ReviewCount).HasColumnName("review_count");
		builder.Property(p => p.Wikipedia).HasColumnName("wikipedia");
		builder.Property(p => p.Wikidata).HasColumnName("wikidata");
		builder.Property(p => p.Wheelchair).HasColumnName("wheelchair");
		builder.Property(p => p.Heritage).HasColumnName("heritage");
		builder.Property(p => p.Fee).HasColumnName("fee");
		builder.Property(p => p.Image).HasColumnName("image");
		builder.Property(p => p.Cuisine).HasColumnName("cuisine");

		var budgetTierConverter = new ValueConverter<List<BudgetTier>, string[]>(
			v => v.Select(x => x.ToString()).ToArray(),
			v => v.Select(x => Enum.Parse<BudgetTier>(x)).ToList());
		var budgetTierComparer = new ValueComparer<List<BudgetTier>>(
			(a, b) => a != null && b != null && a.SequenceEqual(b),
			v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
			v => v.ToList());
		builder.Property(p => p.BudgetTiers)
			.HasColumnName("budget_tiers")
			.HasColumnType("text[]")
			.HasConversion(budgetTierConverter, budgetTierComparer);

		var travelStyleConverter = new ValueConverter<List<TravelStyle>, string[]>(
			v => v.Select(x => x.ToString()).ToArray(),
			v => v.Select(x => Enum.Parse<TravelStyle>(x)).ToList());
		var travelStyleComparer = new ValueComparer<List<TravelStyle>>(
			(a, b) => a != null && b != null && a.SequenceEqual(b),
			v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
			v => v.ToList());
		builder.Property(p => p.TravelStyles)
			.HasColumnName("travel_styles")
			.HasColumnType("text[]")
			.HasConversion(travelStyleConverter, travelStyleComparer);

		var stringListConverter = new ValueConverter<List<string>, string[]>(
			v => v.ToArray(),
			v => v.ToList());
		var stringListComparer = new ValueComparer<List<string>>(
			(a, b) => a != null && b != null && a.SequenceEqual(b),
			v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
			v => v.ToList());

		builder.Property(p => p.PhotoUrls)
			.HasColumnName("photo_urls")
			.HasColumnType("text[]")
			.HasConversion(stringListConverter, stringListComparer);

		builder.Property(p => p.GoogleTags)
			.HasColumnName("google_tags")
			.HasColumnType("text[]")
			.HasConversion(stringListConverter, stringListComparer);

		builder.HasIndex(p => p.BudgetTiers)
			.HasMethod("gin")
			.HasDatabaseName("idx_places_budget_tiers_gin");

		builder.HasIndex(p => p.TravelStyles)
			.HasMethod("gin")
			.HasDatabaseName("idx_places_travel_styles_gin");

		builder.HasIndex(p => p.BestMonths)
			.HasMethod("gin")
			.HasDatabaseName("idx_places_best_months_gin");

		builder.HasIndex(p => p.OpeningHours)
			.HasMethod("gin")
			.HasDatabaseName("idx_places_opening_hours_gin");

		builder.HasIndex(p => p.GoogleTags)
			.HasMethod("gin")
			.HasDatabaseName("idx_places_google_tags_gin");

		builder.HasIndex(p => p.City)
			.HasFilter("is_active = TRUE")
			.HasDatabaseName("idx_places_city");
	}
}
