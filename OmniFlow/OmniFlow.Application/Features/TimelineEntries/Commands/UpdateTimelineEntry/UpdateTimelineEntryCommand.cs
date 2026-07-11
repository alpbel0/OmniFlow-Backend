using MediatR;

namespace OmniFlow.Application.Features.TimelineEntries.Commands.UpdateTimelineEntry;

public record UpdateTimelineEntryCommand(
    Guid Id,
    Guid DestinationId,
    int DayNumber,

    Guid? PlaceId,

    string? CustomName,
    OmniFlow.Domain.Enums.PlaceCategory? CustomCategory,
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

    OmniFlow.Domain.Enums.TransportMode? TransportType,
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
    bool? IsLocked,
    double? TransportFromLatitude = null,
    double? TransportFromLongitude = null,
    double? TransportToLatitude = null,
    double? TransportToLongitude = null
) : IRequest<OmniFlow.Application.DTOs.TimelineEntries.TimelineEntryResponse>;
