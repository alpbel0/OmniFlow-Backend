using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
	public void Configure(EntityTypeBuilder<Flight> builder)
	{
		builder.ToTable("flights", t =>
		{
			t.HasCheckConstraint("iata_from_airport", "from_airport ~ '^[A-Z]{3}$'");
			t.HasCheckConstraint("iata_to_airport", "to_airport ~ '^[A-Z]{3}$'");
			t.HasCheckConstraint("valid_duration", "duration_minutes > 0");
			t.HasCheckConstraint("booked_consistency",
				"is_booked = FALSE OR booked_at IS NOT NULL");
			t.HasCheckConstraint("booking_ref_requires_is_booked",
				"booking_reference IS NULL OR is_booked = TRUE");
		});

		builder.Property(f => f.Id).HasColumnName("id");
		builder.Property(f => f.TripId).HasColumnName("trip_id");
		builder.Property(f => f.ItineraryGroupId).HasColumnName("itinerary_group_id");
		builder.Property(f => f.FlightDirection).HasColumnName("flight_direction").HasConversion<string>();
		builder.Property(f => f.FromCity).HasColumnName("from_city").IsRequired();
		builder.Property(f => f.FromAirport).HasColumnName("from_airport").IsRequired();
		builder.Property(f => f.ToCity).HasColumnName("to_city").IsRequired();
		builder.Property(f => f.ToAirport).HasColumnName("to_airport").IsRequired();
		builder.Property(f => f.DepartureAt).HasColumnName("departure_at")
			.HasColumnType("timestamp without time zone");
		builder.Property(f => f.ArrivalAt).HasColumnName("arrival_at")
			.HasColumnType("timestamp without time zone");
		builder.Property(f => f.DurationMinutes).HasColumnName("duration_minutes");
		builder.Property(f => f.Airline).HasColumnName("airline").IsRequired();
		builder.Property(f => f.FlightNumber).HasColumnName("flight_number").IsRequired();
		builder.Property(f => f.CabinClass).HasColumnName("cabin_class").HasConversion<string>();
		builder.Property(f => f.IsDirect).HasColumnName("is_direct");
		builder.Property(f => f.PricePerPerson).HasColumnName("price_per_person");
		builder.Property(f => f.TotalPrice).HasColumnName("total_price");
		builder.Property(f => f.CurrencyCode).HasColumnName("currency_code").IsRequired();
		builder.Property(f => f.IsBooked).HasColumnName("is_booked").HasDefaultValue(false);
		builder.Property(f => f.BookedAt).HasColumnName("booked_at");
		builder.Property(f => f.BookingReference).HasColumnName("booking_reference");
		builder.Property(f => f.Status).HasColumnName("status").HasConversion<string>();
		builder.Property(f => f.DataSource).HasColumnName("data_source").HasConversion<string>();
		builder.Property(f => f.DataFetchedAt).HasColumnName("data_fetched_at");

		builder.HasOne(f => f.Trip)
			.WithMany(t => t.Flights)
			.HasForeignKey(f => f.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(f => f.ItineraryGroupId)
			.HasDatabaseName("idx_flights_itinerary_group");
	}
}
