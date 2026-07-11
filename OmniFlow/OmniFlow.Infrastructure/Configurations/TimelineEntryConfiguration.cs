using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Configurations;

public class TimelineEntryConfiguration : IEntityTypeConfiguration<TimelineEntry>
{
	public void Configure(EntityTypeBuilder<TimelineEntry> builder)
	{
		builder.ToTable("timeline_entries", t =>
		{
			t.HasCheckConstraint("entry_type_place_requires_id",
				"entry_type = 'Place' AND place_id IS NOT NULL OR entry_type != 'Place'");
			t.HasCheckConstraint("custom_flight_requires_fields",
				"entry_type != 'CustomFlight' OR (flight_from_airport IS NOT NULL AND flight_to_airport IS NOT NULL AND flight_departure_at IS NOT NULL AND flight_arrival_at IS NOT NULL)");
			t.HasCheckConstraint("custom_transport_requires_type",
				"entry_type != 'CustomTransport' OR transport_type IS NOT NULL");
			t.HasCheckConstraint("custom_accommodation_requires_dates",
				"entry_type != 'CustomAccommodation' OR (accommodation_check_in IS NOT NULL AND accommodation_check_out IS NOT NULL)");
			t.HasCheckConstraint("custom_event_requires_time",
				"entry_type != 'CustomEvent' OR (start_time IS NOT NULL AND duration_minutes IS NOT NULL)");
			t.HasCheckConstraint("locked_entry_has_buffer",
				"is_locked = FALSE OR buffer_minutes IS NOT NULL");
			t.HasCheckConstraint("valid_order_index",
				"order_index > 0");
		});

		builder.Property(e => e.Id).HasColumnName("id");
		builder.Property(e => e.TripId).HasColumnName("trip_id").IsRequired();
		builder.Property(e => e.DestinationId).HasColumnName("destination_id").IsRequired();
		builder.Property(e => e.DayNumber).HasColumnName("day_number").IsRequired();
		builder.Property(e => e.OrderIndex).HasColumnName("order_index").HasColumnType("double precision").IsRequired();
		builder.Property(e => e.EntryType).HasColumnName("entry_type").HasConversion<string>().IsRequired();
		builder.Property(e => e.PlanningSlotKey).HasColumnName("planning_slot_key").HasMaxLength(160);

		// Place
		builder.Property(e => e.PlaceId).HasColumnName("place_id");

		// Custom common
		builder.Property(e => e.CustomName).HasColumnName("custom_name");
		builder.Property(e => e.CustomCategory).HasColumnName("custom_category").HasConversion<string>();
		builder.Property(e => e.CustomPhotoUrl).HasColumnName("custom_photo_url");
		builder.Property(e => e.CustomLatitude).HasColumnName("custom_latitude");
		builder.Property(e => e.CustomLongitude).HasColumnName("custom_longitude");
		builder.Property(e => e.CustomDescription).HasColumnName("custom_description");

		// Timing & Locking
		builder.Property(e => e.StartTime).HasColumnName("start_time").HasColumnType("time");
		builder.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
		builder.Property(e => e.IsLocked).HasColumnName("is_locked").HasDefaultValue(false);
		builder.Property(e => e.BufferMinutes).HasColumnName("buffer_minutes");

		// CustomFlight specific
		builder.Property(e => e.FlightFromAirport).HasColumnName("flight_from_airport");
		builder.Property(e => e.FlightToAirport).HasColumnName("flight_to_airport");
		builder.Property(e => e.FlightFromCity).HasColumnName("flight_from_city");
		builder.Property(e => e.FlightToCity).HasColumnName("flight_to_city");
		builder.Property(e => e.FlightDepartureAt).HasColumnName("flight_departure_at");
		builder.Property(e => e.FlightArrivalAt).HasColumnName("flight_arrival_at");
		builder.Property(e => e.Airline).HasColumnName("airline");
		builder.Property(e => e.FlightNumber).HasColumnName("flight_number");

		// CustomTransport specific
		builder.Property(e => e.TransportType).HasColumnName("transport_type").HasConversion<string>();
		builder.Property(e => e.TransportFromStation).HasColumnName("transport_from_station");
		builder.Property(e => e.TransportToStation).HasColumnName("transport_to_station");
		builder.Property(e => e.TransportCompany).HasColumnName("transport_company");
		builder.Property(e => e.TransportFromLatitude).HasColumnName("transport_from_latitude");
		builder.Property(e => e.TransportFromLongitude).HasColumnName("transport_from_longitude");
		builder.Property(e => e.TransportToLatitude).HasColumnName("transport_to_latitude");
		builder.Property(e => e.TransportToLongitude).HasColumnName("transport_to_longitude");

		// CustomAccommodation specific
		builder.Property(e => e.AccommodationCheckIn).HasColumnName("accommodation_check_in");
		builder.Property(e => e.AccommodationCheckOut).HasColumnName("accommodation_check_out");
		builder.Property(e => e.AccommodationAddress).HasColumnName("accommodation_address");

		// Pricing
		builder.Property(e => e.Price).HasColumnName("price").HasDefaultValue(0m);
		builder.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasDefaultValue("USD");

		// Provider references
		builder.Property(e => e.ProviderFlightId).HasColumnName("provider_flight_id");
		builder.Property(e => e.ProviderHotelId).HasColumnName("provider_hotel_id");

		// Extra info
		builder.Property(e => e.Notes).HasColumnName("notes");
		builder.Property(e => e.IsVisited).HasColumnName("is_visited").HasDefaultValue(false);
		builder.Property(e => e.VisitedAt).HasColumnName("visited_at");
		builder.Property(e => e.AddedBy).HasColumnName("added_by").HasConversion<string>();
		builder.Property(e => e.AiReasoning).HasColumnName("ai_reasoning");

		// AuditableBaseEntity columns
		builder.Property(e => e.CreatedAt).HasColumnName("created_at");
		builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
		builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

		// === Foreign Keys ===
		builder.HasOne(e => e.Trip)
			.WithMany()
			.HasForeignKey(e => e.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(e => e.Destination)
			.WithMany(d => d.TimelineEntries)
			.HasForeignKey(e => e.DestinationId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(e => e.Place)
			.WithMany()
			.HasForeignKey(e => e.PlaceId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(e => e.ProviderFlight)
			.WithMany()
			.HasForeignKey(e => e.ProviderFlightId)
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(e => e.ProviderHotel)
			.WithMany()
			.HasForeignKey(e => e.ProviderHotelId)
			.OnDelete(DeleteBehavior.SetNull);

		// === Indexes ===
		builder.HasIndex(e => new { e.TripId, e.DestinationId, e.DayNumber, e.OrderIndex })
			.HasDatabaseName("idx_timeline_entries_trip_dest_day_order");

		builder.HasIndex(e => new { e.TripId, e.PlanningSlotKey })
			.HasFilter("planning_slot_key IS NOT NULL AND deleted_at IS NULL")
			.IsUnique()
			.HasDatabaseName("ux_timeline_entries_trip_planning_slot_active");

		builder.HasIndex(e => e.PlaceId)
			.HasFilter("place_id IS NOT NULL")
			.HasDatabaseName("idx_timeline_entries_place");

		builder.HasIndex(e => e.ProviderFlightId)
			.HasFilter("provider_flight_id IS NOT NULL")
			.HasDatabaseName("idx_timeline_entries_provider_flight");

		builder.HasIndex(e => e.ProviderHotelId)
			.HasFilter("provider_hotel_id IS NOT NULL")
			.HasDatabaseName("idx_timeline_entries_provider_hotel");
	}
}
