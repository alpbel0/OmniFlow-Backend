using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Flights;

public class FlightResponse
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid? ItineraryGroupId { get; set; }

    // Direction
    public FlightDirection FlightDirection { get; set; }

    // Route information
    public string FromCity { get; set; } = string.Empty;
    public string FromAirport { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public string ToAirport { get; set; } = string.Empty;

    // Timing
    public DateTime DepartureAt { get; set; }
    public DateTime ArrivalAt { get; set; }
    public int DurationMinutes { get; set; }

    // Flight details
    public string Airline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public CabinClass CabinClass { get; set; }
    public bool IsDirect { get; set; }

    // Pricing
    public decimal PricePerPerson { get; set; }
    public decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;

    // Booking status
    public bool IsBooked { get; set; }
    public DateTime? BookedAt { get; set; }
    public string? BookingReference { get; set; }

    // Status
    public FlightStatus Status { get; set; }
    public FlightDataSource DataSource { get; set; }
}