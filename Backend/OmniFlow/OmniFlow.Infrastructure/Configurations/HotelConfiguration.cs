using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
	public void Configure(EntityTypeBuilder<Hotel> builder)
	{
		builder.ToTable("hotels", t =>
		{
			t.HasCheckConstraint("place_or_hotel_name",
				"place_id IS NOT NULL OR hotel_name IS NOT NULL");
			t.HasCheckConstraint("valid_dates", "check_out > check_in");
			t.HasCheckConstraint("valid_stars",
				"stars IS NULL OR (stars >= 1 AND stars <= 5)");
			t.HasCheckConstraint("booked_consistency",
				"is_booked = FALSE OR booked_at IS NOT NULL");
		});

		builder.Property(h => h.Id).HasColumnName("id");
		builder.Property(h => h.TripId).HasColumnName("trip_id");
		builder.Property(h => h.PlaceId).HasColumnName("place_id");
		builder.Property(h => h.HotelName).HasColumnName("hotel_name");
		builder.Property(h => h.HotelLatitude).HasColumnName("hotel_latitude");
		builder.Property(h => h.HotelLongitude).HasColumnName("hotel_longitude");
		builder.Property(h => h.HotelAddress).HasColumnName("hotel_address");
		builder.Property(h => h.HotelPhone).HasColumnName("hotel_phone");
		builder.Property(h => h.ProviderUrl).HasColumnName("provider_url");
		builder.Property(h => h.Stars).HasColumnName("stars");
		builder.Property(h => h.RoomType).HasColumnName("room_type").HasConversion<string>();
		builder.Property(h => h.BreakfastIncluded).HasColumnName("breakfast_included");
		builder.Property(h => h.CancellationPolicy).HasColumnName("cancellation_policy").HasConversion<string>();
		builder.Property(h => h.CheckIn).HasColumnName("check_in")
			.HasColumnType("timestamp without time zone");
		builder.Property(h => h.CheckOut).HasColumnName("check_out")
			.HasColumnType("timestamp without time zone");
		builder.Property(h => h.PricePerNight).HasColumnName("price_per_night");
		builder.Property(h => h.TotalPrice).HasColumnName("total_price");
		builder.Property(h => h.CurrencyCode).HasColumnName("currency_code").IsRequired();
		builder.Property(h => h.IsBooked).HasColumnName("is_booked").HasDefaultValue(false);
		builder.Property(h => h.BookedAt).HasColumnName("booked_at");
		builder.Property(h => h.BookingReference).HasColumnName("booking_reference");
		builder.Property(h => h.Status).HasColumnName("status").HasConversion<string>();
		builder.Property(h => h.DataSource).HasColumnName("data_source").HasConversion<string>();
		builder.Property(h => h.DataFetchedAt).HasColumnName("data_fetched_at");

		builder.HasOne(h => h.Trip)
			.WithMany(t => t.Hotels)
			.HasForeignKey(h => h.TripId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(h => h.Place)
			.WithMany()
			.HasForeignKey(h => h.PlaceId)
			.OnDelete(DeleteBehavior.SetNull);

	}
}
