using OmniFlow.Application.DTOs.Geocoding;

namespace OmniFlow.Application.Interfaces;

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(
        string city,
        string country,
        CancellationToken cancellationToken = default);

    Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>Free-text city search for autocomplete (e.g. "Rom" -> Rome, Italy). Not cached -
    /// callers should debounce on the client since each call may hit the provider.</summary>
    Task<IReadOnlyList<GeocodingResult>> SearchCitiesAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default);
}
