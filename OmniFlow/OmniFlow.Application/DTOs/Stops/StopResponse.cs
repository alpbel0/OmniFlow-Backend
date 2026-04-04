using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Stops;

public class StopResponse
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public int DayNumber { get; set; }
    public double OrderIndex { get; set; }

    // Time fields
    public TimeOnly? ArrivalTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsTimeLocked { get; set; }

    // Place reference (nullable)
    public Guid? PlaceId { get; set; }
    public string? PlaceName { get; set; }
    public PlaceCategory? PlaceCategory { get; set; }
    public string? PlacePhotoUrl { get; set; }

    // Fallback place (nullable)
    public Guid? FallbackPlaceId { get; set; }
    public string? FallbackPlaceName { get; set; }
    public PlaceCategory? FallbackPlaceCategory { get; set; }

    // Custom stop fields (when PlaceId is null)
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
    public decimal ActivityPrice { get; set; }
    public decimal TransportPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Transport
    public TransportMode? TransportFromPrevious { get; set; }
    public int? TravelTimeFromPrevious { get; set; }

    // Visited tracking
    public bool IsVisited { get; set; }
    public DateTime? VisitedAt { get; set; }

    // Metadata
    public StopAddedBy AddedBy { get; set; }
    public string? AiReasoning { get; set; }
}