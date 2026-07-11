namespace OmniFlow.Application.DTOs.TimelineEntries;

/// <summary>
/// Common validation properties shared between Create and Update TimelineEntry DTOs.
/// Enables DRY validation rules via TimelineEntryBaseValidator.
/// </summary>
public interface ITimelineEntryValidationProperties
{
    decimal Price { get; }
    string? CurrencyCode { get; }
    double? CustomLatitude { get; }
    double? CustomLongitude { get; }
    double? TransportFromLatitude { get; }
    double? TransportFromLongitude { get; }
    double? TransportToLatitude { get; }
    double? TransportToLongitude { get; }
    string? CustomPhotoUrl { get; }
    string? FlightFromAirport { get; }
    string? FlightToAirport { get; }
    DateTime? FlightDepartureAt { get; }
    DateTime? FlightArrivalAt { get; }
    DateTime? AccommodationCheckIn { get; }
    DateTime? AccommodationCheckOut { get; }
    int? DurationMinutes { get; }
    string? CustomName { get; }
}
