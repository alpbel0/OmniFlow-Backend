using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class TimelineEntryResponse
{
    // Core
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid DestinationId { get; set; }
    public int DayNumber { get; set; }
    public TimelineEntryType EntryType { get; set; }
    public double OrderIndex { get; set; }

    // Place
    public Guid? PlaceId { get; set; }

    // Custom common
    public string? CustomName { get; set; }
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }
    public string? CustomDescription { get; set; }

    // Timing & Locking
    public TimeOnly? StartTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsLocked { get; set; }
    public int? BufferMinutes { get; set; }

    // CustomFlight
    public string? FlightFromAirport { get; set; }
    public string? FlightToAirport { get; set; }
    public string? FlightFromCity { get; set; }
    public string? FlightToCity { get; set; }
    public DateTime? FlightDepartureAt { get; set; }
    public DateTime? FlightArrivalAt { get; set; }
    public string? Airline { get; set; }
    public string? FlightNumber { get; set; }

    // CustomTransport
    public TransportMode? TransportType { get; set; }
    public string? TransportFromStation { get; set; }
    public string? TransportToStation { get; set; }
    public string? TransportCompany { get; set; }

    // CustomAccommodation
    public DateTime? AccommodationCheckIn { get; set; }
    public DateTime? AccommodationCheckOut { get; set; }
    public string? AccommodationAddress { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // Provider
    public Guid? ProviderFlightId { get; set; }
    public Guid? ProviderHotelId { get; set; }

    // Extra
    public string? Notes { get; set; }
    public bool IsVisited { get; set; }
    public DateTime? VisitedAt { get; set; }
    public StopAddedBy AddedBy { get; set; }
    public string? AiReasoning { get; set; }
}