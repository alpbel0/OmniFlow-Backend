using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.CreateTimelineEntry;

public record CreateTimelineEntryCommand(
    Guid TripId,
    Guid DestinationId,
    int DayNumber,
    TimelineEntryType EntryType,

    Guid? PlaceId,

    string? CustomName,
    PlaceCategory? CustomCategory,
    string? CustomPhotoUrl,
    double? CustomLatitude,
    double? CustomLongitude,
    string? CustomDescription,

    TimeOnly? StartTime,
    int? DurationMinutes,

    string? FlightFromAirport,
    string? FlightToAirport,
    string? FlightFromCity,
    string? FlightToCity,
    DateTime? FlightDepartureAt,
    DateTime? FlightArrivalAt,
    string? Airline,
    string? FlightNumber,

    TransportMode? TransportType,
    string? TransportFromStation,
    string? TransportToStation,
    string? TransportCompany,

    DateTime? AccommodationCheckIn,
    DateTime? AccommodationCheckOut,
    string? AccommodationAddress,

    decimal Price,
    string? CurrencyCode,

    Guid? ProviderFlightId,
    Guid? ProviderHotelId,

    string? Notes,
    string? PlanningSlotKey = null,
    bool? IsLocked = null
) : IRequest<OmniFlow.Application.DTOs.TimelineEntries.TimelineEntryResponse>;
