using OmniFlow.Application.DTOs.Geocoding;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.IntegrationTests.Setup;

public class TestGeocodingService : IGeocodingService
{
    public Task<GeocodingResult?> GeocodeAsync(
        string city,
        string country,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(city.Trim(), "Unknown Origin", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<GeocodingResult?>(null);

        return Task.FromResult<GeocodingResult?>(new GeocodingResult
        {
            Latitude = 41.0082,
            Longitude = 28.9784,
            DisplayName = $"{city.Trim()}, {country.Trim()}",
            City = city.Trim(),
            Country = country.Trim()
        });
    }

    public Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<GeocodingResult?>(new GeocodingResult
        {
            Latitude = latitude,
            Longitude = longitude,
            DisplayName = "Istanbul, Turkiye",
            City = "Istanbul",
            Country = "Turkiye"
        });
    }
}
