using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Hotel : BaseEntity
{
	public Guid TripId { get; set; }

	public Guid? PlaceId { get; set; }

	public string? HotelName { get; set; }

	public double? HotelLatitude { get; set; }

	public double? HotelLongitude { get; set; }

	public string? HotelAddress { get; set; }

	public string? HotelPhone { get; set; }

	public string? ProviderUrl { get; set; }

	public int? Stars { get; set; }

	public RoomType RoomType { get; set; }

	public bool BreakfastIncluded { get; set; }

	public CancellationPolicy CancellationPolicy { get; set; }

	public DateTime CheckIn { get; set; }

	public DateTime CheckOut { get; set; }

	public decimal PricePerNight { get; set; }

	public decimal TotalPrice { get; set; }

	public string CurrencyCode { get; set; } = string.Empty;

	public bool IsBooked { get; set; }

	public DateTime? BookedAt { get; set; }

	public string? BookingReference { get; set; }

	public HotelStatus Status { get; set; }

	public HotelDataSource DataSource { get; set; }

	public DateTime DataFetchedAt { get; set; }

	public Trip? Trip { get; set; }

	public Place? Place { get; set; }
}
