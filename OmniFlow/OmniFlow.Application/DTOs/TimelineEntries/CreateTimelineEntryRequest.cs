using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class CreateTimelineEntryRequest : ITimelineEntryValidationProperties
{
    public Guid TripId { get; set; }
    public Guid DestinationId { get; set; }
    public int DayNumber { get; set; }
    public TimelineEntryType EntryType { get; set; }
    public string? PlanningSlotKey { get; set; }
    public bool? IsLocked { get; set; }

    public Guid? PlaceId { get; set; }

    // Custom
    public string? CustomName { get; set; }
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }
    public string? CustomDescription { get; set; }

    // Timing
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }

    // Flight
    public string? FlightFromAirport { get; set; }
    public string? FlightToAirport { get; set; }
    public string? FlightFromCity { get; set; }
    public string? FlightToCity { get; set; }
    public DateTime? FlightDepartureAt { get; set; }
    public DateTime? FlightArrivalAt { get; set; }
    public string? Airline { get; set; }
    public string? FlightNumber { get; set; }

    // Transport
    public TransportMode? TransportType { get; set; }
    public string? TransportFromStation { get; set; }
    public string? TransportToStation { get; set; }
    public string? TransportCompany { get; set; }

    // Accommodation
    public DateTime? AccommodationCheckIn { get; set; }
    public DateTime? AccommodationCheckOut { get; set; }
    public string? AccommodationAddress { get; set; }

    // Pricing
    public decimal Price { get; set; } = 0;
    public string CurrencyCode { get; set; } = "USD";

    // Provider
    public Guid? ProviderFlightId { get; set; }
    public Guid? ProviderHotelId { get; set; }

    // Extra
    public string? Notes { get; set; }
}
