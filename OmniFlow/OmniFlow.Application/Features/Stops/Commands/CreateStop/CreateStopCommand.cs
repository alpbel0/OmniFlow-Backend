using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Stops.Commands.CreateStop;

public class CreateStopCommand : IRequest<Guid>
{
    public Guid TripId { get; set; }

    // Place reference (nullable - if null, CustomName required)
    public Guid? PlaceId { get; set; }
    public Guid? FallbackPlaceId { get; set; }

    // Day and time
    public int DayNumber { get; set; }
    public TimeOnly? ArrivalTime { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsTimeLocked { get; set; }

    // Custom stop fields (required if PlaceId is null)
    public string? CustomName { get; set; }
    public PlaceCategory? CustomCategory { get; set; }
    public string? CustomPhotoUrl { get; set; }
    public double? CustomLatitude { get; set; }
    public double? CustomLongitude { get; set; }

    // Optional fields
    public string? Notes { get; set; }
    public decimal ActivityPrice { get; set; } = 0;
    public decimal TransportPrice { get; set; } = 0;
    public string? CurrencyCode { get; set; }
    public TransportMode? TransportFromPrevious { get; set; }
    public int? TravelTimeFromPrevious { get; set; }
}