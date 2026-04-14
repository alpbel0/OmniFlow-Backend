using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class ProviderFlightConfiguration : IEntityTypeConfiguration<ProviderFlight>
{
	public void Configure(EntityTypeBuilder<ProviderFlight> builder)
	{
		builder.ToTable("provider_flights", t =>
		{
			t.HasCheckConstraint("valid_provider_flight_departure_airport",
				"departure_airport_code ~ '^[A-Z]{3}$'");
			t.HasCheckConstraint("valid_provider_flight_arrival_airport",
				"arrival_airport_code ~ '^[A-Z]{3}$'");
			t.HasCheckConstraint("valid_provider_flight_times",
				"arrival_time > departure_time");
			t.HasCheckConstraint("valid_provider_flight_duration",
				"duration_minutes > 0");
			t.HasCheckConstraint("valid_provider_flight_price",
				"price >= 0");
			t.HasCheckConstraint("valid_provider_flight_seats",
				"available_seats IS NULL OR available_seats >= 0");
			t.HasCheckConstraint("valid_provider_flight_currency",
				"char_length(currency_code) = 3");
		});

		builder.Property(f => f.Id).HasColumnName("id");
		builder.Property(f => f.FlightNumber).HasColumnName("flight_number").HasMaxLength(20).IsRequired();
		builder.Property(f => f.Airline).HasColumnName("airline").HasMaxLength(100).IsRequired();
		builder.Property(f => f.AirlineLogoUrl).HasColumnName("airline_logo_url").HasMaxLength(1000);
		builder.Property(f => f.DepartureCity).HasColumnName("departure_city").HasMaxLength(100).IsRequired();
		builder.Property(f => f.ArrivalCity).HasColumnName("arrival_city").HasMaxLength(100).IsRequired();
		builder.Property(f => f.DepartureAirportCode).HasColumnName("departure_airport_code").HasMaxLength(10).IsRequired();
		builder.Property(f => f.ArrivalAirportCode).HasColumnName("arrival_airport_code").HasMaxLength(10).IsRequired();
		builder.Property(f => f.DepartureTime).HasColumnName("departure_time")
			.HasColumnType("timestamp without time zone");
		builder.Property(f => f.ArrivalTime).HasColumnName("arrival_time")
			.HasColumnType("timestamp without time zone");
		builder.Property(f => f.DurationMinutes).HasColumnName("duration_minutes");
		builder.Property(f => f.Price).HasColumnName("price").HasPrecision(18, 2);
		builder.Property(f => f.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
		builder.Property(f => f.AvailableSeats).HasColumnName("available_seats");
		builder.Property(f => f.ProviderName).HasColumnName("provider_name").HasMaxLength(50).IsRequired();

		builder.HasIndex(f => new { f.DepartureCity, f.ArrivalCity, f.DepartureTime })
			.HasDatabaseName("idx_provider_flights_route_departure_time");

		builder.HasIndex(f => f.ProviderName)
			.HasDatabaseName("idx_provider_flights_provider_name");
	}
}
