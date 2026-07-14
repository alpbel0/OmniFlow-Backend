using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Configurations;

public class TripConfiguration : IEntityTypeConfiguration<Trip>
{
	public void Configure(EntityTypeBuilder<Trip> builder)
	{
		builder.ToTable("trips", t =>
		{
			t.HasCheckConstraint("valid_dates", "end_date >= start_date");
			t.HasCheckConstraint("valid_person_count", "person_count >= 1");
			t.HasCheckConstraint("non_negative_counts",
				"fork_count >= 0 AND upvote_count >= 0 AND view_count >= 0");
		});

		builder.Property(t => t.Id).HasColumnName("id");
		builder.Property(t => t.OwnerId).HasColumnName("owner_id");
		builder.Property(t => t.ForkedFromId).HasColumnName("forked_from_id");
		builder.Property(t => t.Title).HasColumnName("title").IsRequired();
		builder.Property(t => t.Description).HasColumnName("description");
		builder.Property(t => t.CoverPhotoUrl).HasColumnName("cover_photo_url");
		builder.Property(t => t.Status).HasColumnName("status").HasConversion<string>();

		// Origin fields (replaced City/Country)
		builder.Property(t => t.Origin).HasColumnName("origin").IsRequired();
		builder.Property(t => t.OriginCountry).HasColumnName("origin_country").IsRequired();
		builder.Property(t => t.OriginLatitude).HasColumnName("origin_latitude");
		builder.Property(t => t.OriginLongitude).HasColumnName("origin_longitude");

		builder.Property(t => t.StartDate).HasColumnName("start_date");
		builder.Property(t => t.EndDate).HasColumnName("end_date");
		builder.Property(t => t.PersonCount).HasColumnName("person_count").HasDefaultValue(1);
		builder.Property(t => t.BudgetTier).HasColumnName("budget_tier").HasConversion<string>();

		// TravelStyles as PostgreSQL text[] with explicit enum converter
		var travelStyleConverter = new ValueConverter<List<TravelStyle>, string[]>(
			v => v.Select(e => e.ToString()).ToArray(),
			v => v.Select(s => Enum.Parse<TravelStyle>(s)).ToList()
		);
		var travelStyleComparer = new ValueComparer<List<TravelStyle>>(
			(a, b) => a != null && b != null && a.SequenceEqual(b),
			v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
			v => v.ToList());
		builder.Property(t => t.TravelStyles)
			.HasColumnName("travel_styles")
			.HasColumnType("text[]")
			.HasConversion(travelStyleConverter, travelStyleComparer);

		// Wizard fields
		builder.Property(t => t.TravelCompanion)
			.HasColumnName("travel_companion")
			.HasConversion<string>()
			.IsRequired();
		builder.Property(t => t.Tempo)
			.HasColumnName("tempo")
			.HasConversion<string>()
			.IsRequired();
		builder.Property(t => t.TransportPreference)
			.HasColumnName("transport_preference")
			.HasConversion<string>()
			.IsRequired();
		builder.Property(t => t.ManualBudget).HasColumnName("manual_budget");
		builder.Property(t => t.AdjustedBudgetTier)
			.HasColumnName("adjusted_budget_tier")
			.HasConversion<string>();

		builder.Property(t => t.EstimatedCost).HasColumnName("estimated_cost");
		builder.Property(t => t.BaseCurrencyCode).HasColumnName("base_currency_code").HasMaxLength(3).HasDefaultValue("USD").IsRequired();
		builder.Property(t => t.ForkCount).HasColumnName("fork_count").HasDefaultValue(0);
		builder.Property(t => t.UpvoteCount).HasColumnName("upvote_count").HasDefaultValue(0);
		builder.Property(t => t.ViewCount).HasColumnName("view_count").HasDefaultValue(0);
		builder.Property(t => t.PopularityScore).HasColumnName("popularity_score").HasDefaultValue(0m);
		builder.Property(t => t.Tags).HasColumnName("tags").HasColumnType("text[]");
		builder.Property(t => t.CreatedAt).HasColumnName("created_at");
		builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
		builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");

		// Navigation: Owner
		builder.HasOne(t => t.Owner)
			.WithMany(u => u.Trips)
			.HasForeignKey(t => t.OwnerId)
			.OnDelete(DeleteBehavior.Restrict);

		// Navigation: ForkedFrom
		builder.HasOne<Trip>()
			.WithMany()
			.HasForeignKey(t => t.ForkedFromId)
			.OnDelete(DeleteBehavior.SetNull);

		// Navigation: Destinations
		builder.HasMany(t => t.Destinations)
			.WithOne(d => d.Trip)
			.HasForeignKey(d => d.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		// Navigation: TimelineEntries
		builder.HasMany(t => t.TimelineEntries)
			.WithOne(e => e.Trip)
			.HasForeignKey(e => e.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		// Navigation: Flights
		builder.HasMany(t => t.Flights)
			.WithOne(f => f.Trip)
			.HasForeignKey(f => f.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		// Navigation: Hotels
		builder.HasMany(t => t.Hotels)
			.WithOne(h => h.Trip)
			.HasForeignKey(h => h.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		// Indexes
		builder.HasIndex(t => t.Tags)
			.HasMethod("gin")
			.HasDatabaseName("idx_trips_tags_gin");

		builder.HasIndex(t => t.Status)
			.HasFilter("status = 'Published' AND deleted_at IS NULL")
			.HasDatabaseName("idx_trips_explore");
	}
}
