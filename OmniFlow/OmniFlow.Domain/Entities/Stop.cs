using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Stop : AuditableBaseEntity
{
	public Guid TripId { get; set; }

	public Guid? PlaceId { get; set; }

	public Guid? FallbackPlaceId { get; set; }

	public int DayNumber { get; set; }

	public double OrderIndex { get; set; }

	public TimeOnly? ArrivalTime { get; set; }

	public int? DurationMinutes { get; set; }

	public bool IsTimeLocked { get; set; }

	public string? CustomName { get; set; }

	public PlaceCategory? CustomCategory { get; set; }

	public string? CustomPhotoUrl { get; set; }

	public double? CustomLatitude { get; set; }

	public double? CustomLongitude { get; set; }

	public string? Notes { get; set; }

	public string? BookingReference { get; set; }

	public string? ReservationNote { get; set; }

	public decimal ActivityPrice { get; set; } = 0;

	public decimal TransportPrice { get; set; } = 0;

	public string CurrencyCode { get; set; } = string.Empty;

	public TransportMode? TransportFromPrevious { get; set; }

	public int? TravelTimeFromPrevious { get; set; }

	public bool IsVisited { get; set; }

	public DateTime? VisitedAt { get; set; }

	public StopAddedBy AddedBy { get; set; } = StopAddedBy.User;

	public string? AiReasoning { get; set; }

	public Trip? Trip { get; set; }

	public Place? Place { get; set; }

	public Place? FallbackPlace { get; set; }
}
