using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Stops;

public class UpdateStopRequest
{
    // Place reference
    public Guid? PlaceId { get; set; }
    public Guid? FallbackPlaceId { get; set; }

    // Day and time
    public int? DayNumber { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool? IsTimeLocked { get; set; }

    // Custom stop fields
    public string? CustomName { get; set; }
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }

    // Notes and booking
    public string? Notes { get; set; }
    public string? BookingReference { get; set; }
    public string? ReservationNote { get; set; }

    // Pricing
    public decimal? ActivityPrice { get; set; }
    public decimal? TransportPrice { get; set; }
    public string? CurrencyCode { get; set; }

    // Transport
    public TransportMode? TransportFromPrevious { get; set; }
    public int? TravelTimeFromPrevious { get; set; }
}