using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class StopConfiguration : IEntityTypeConfiguration<Stop>
{
	public void Configure(EntityTypeBuilder<Stop> builder)
	{
		builder.ToTable("stops", t =>
		{
			t.HasCheckConstraint("place_or_custom_name",
				"place_id IS NOT NULL OR custom_name IS NOT NULL");
			t.HasCheckConstraint("custom_place_requires_category",
				"custom_name IS NULL OR custom_category IS NOT NULL");
			t.HasCheckConstraint("time_lock_requires_arrival",
				"is_time_locked = FALSE OR arrival_time IS NOT NULL");
			t.HasCheckConstraint("visited_consistency",
				"is_visited = FALSE OR visited_at IS NOT NULL");
			t.HasCheckConstraint("fallback_differs_from_place",
				"fallback_place_id IS NULL OR fallback_place_id != place_id");
			t.HasCheckConstraint("ai_reasoning_required",
				"added_by != 'Ai' OR ai_reasoning IS NOT NULL");
		});

		builder.Property(s => s.Id).HasColumnName("id");
		builder.Property(s => s.TripId).HasColumnName("trip_id");
		builder.Property(s => s.PlaceId).HasColumnName("place_id");
		builder.Property(s => s.FallbackPlaceId).HasColumnName("fallback_place_id");
		builder.Property(s => s.DayNumber).HasColumnName("day_number");
		builder.Property(s => s.OrderIndex).HasColumnName("order_index").HasColumnType("double precision");
		builder.Property(s => s.ArrivalTime).HasColumnName("arrival_time");
		builder.Property(s => s.DurationMinutes).HasColumnName("duration_minutes");
		builder.Property(s => s.IsTimeLocked).HasColumnName("is_time_locked").HasDefaultValue(false);
		builder.Property(s => s.CustomName).HasColumnName("custom_name");
		builder.Property(s => s.CustomCategory).HasColumnName("custom_category").HasConversion<string>();
		builder.Property(s => s.CustomPhotoUrl).HasColumnName("custom_photo_url");
		builder.Property(s => s.CustomLatitude).HasColumnName("custom_latitude");
		builder.Property(s => s.CustomLongitude).HasColumnName("custom_longitude");
		builder.Property(s => s.Notes).HasColumnName("notes");
		builder.Property(s => s.BookingReference).HasColumnName("booking_reference");
		builder.Property(s => s.ReservationNote).HasColumnName("reservation_note");
		builder.Property(s => s.ActivityPrice).HasColumnName("activity_price").HasDefaultValue(0m);
		builder.Property(s => s.TransportPrice).HasColumnName("transport_price").HasDefaultValue(0m);
		builder.Property(s => s.CurrencyCode).HasColumnName("currency_code");
		builder.Property(s => s.TransportFromPrevious).HasColumnName("transport_from_previous").HasConversion<string>();
		builder.Property(s => s.TravelTimeFromPrevious).HasColumnName("travel_time_from_previous");
		builder.Property(s => s.IsVisited).HasColumnName("is_visited").HasDefaultValue(false);
		builder.Property(s => s.VisitedAt).HasColumnName("visited_at");
		builder.Property(s => s.AddedBy).HasColumnName("added_by").HasConversion<string>();
		builder.Property(s => s.AiReasoning).HasColumnName("ai_reasoning");
		builder.Property(s => s.CreatedAt).HasColumnName("created_at");
		builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");
		builder.Property(s => s.DeletedAt).HasColumnName("deleted_at");

		builder.HasOne(s => s.Trip)
			.WithMany(t => t.Stops)
			.HasForeignKey(s => s.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(s => s.Place)
			.WithMany()
			.HasForeignKey(s => s.PlaceId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(s => s.FallbackPlace)
			.WithMany()
			.HasForeignKey(s => s.FallbackPlaceId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(s => new { s.TripId, s.DayNumber, s.OrderIndex })
			.HasDatabaseName("idx_stops_trip_day_order");
	}
}
