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
}
