using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Flight : BaseEntity
{
	public Guid TripId { get; set; }

	public Guid? ItineraryGroupId { get; set; }

	public FlightDirection FlightDirection { get; set; }

	public string FromCity { get; set; } = string.Empty;

	public string FromAirport { get; set; } = string.Empty;

	public string ToCity { get; set; } = string.Empty;

	public string ToAirport { get; set; } = string.Empty;

	public DateTime DepartureAt { get; set; }

	public DateTime ArrivalAt { get; set; }

	public int DurationMinutes { get; set; }

	public string Airline { get; set; } = string.Empty;

	public string FlightNumber { get; set; } = string.Empty;

	public CabinClass CabinClass { get; set; }

	public bool IsDirect { get; set; }

	public decimal PricePerPerson { get; set; }

	public decimal TotalPrice { get; set; }

	public string CurrencyCode { get; set; } = string.Empty;

	public bool IsBooked { get; set; }

	public DateTime? BookedAt { get; set; }

	public string? BookingReference { get; set; }

	public FlightStatus Status { get; set; }

	public FlightDataSource DataSource { get; set; }

	public DateTime DataFetchedAt { get; set; }

	public Trip? Trip { get; set; }
}
