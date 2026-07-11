using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniFlow.Application.DTOs.Geocoding;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Settings;

namespace OmniFlow.Infrastructure.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private const string ProviderName = "nominatim";
    private static readonly SemaphoreSlim RequestGate = new(1, 1);
    private static DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

    private readonly HttpClient _httpClient;
    private readonly IApplicationDbContext _context;
    private readonly GeocodingSettings _settings;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        IApplicationDbContext context,
        IOptions<GeocodingSettings> settings,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string city,
        string country,
        CancellationToken cancellationToken = default)
    {
        var key = BuildForwardKey(city, country);
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var cached = await _context.GeocodingCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.Provider == ProviderName && entry.ForwardKey == key,
                cancellationToken);

        if (cached is not null)
            return ToResult(cached);

        try
        {
            await WaitForRateLimitAsync(cancellationToken);

            var url =
                $"/search?format=jsonv2&limit=1&city={Uri.EscapeDataString(city.Trim())}&country={Uri.EscapeDataString(country.Trim())}";

            var response = await _httpClient.GetFromJsonAsync<List<NominatimSearchResponse>>(url, cancellationToken);
            var first = response?.FirstOrDefault();
            if (first is null ||
                !double.TryParse(first.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(first.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                return null;
            }

            var result = new GeocodingResult
            {
                Latitude = latitude,
                Longitude = longitude,
                DisplayName = first.DisplayName,
                City = city.Trim(),
                Country = country.Trim()
            };

            await CacheForwardAsync(key, result, cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Forward geocoding failed for {City}, {Country}", city, country);
            return null;
        }
    }

    public async Task<IReadOnlyList<GeocodingResult>> SearchCitiesAsync(
        string query,
        int limit = 8,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<GeocodingResult>();

        try
        {
            await WaitForRateLimitAsync(cancellationToken);

            var url =
                $"/search?format=jsonv2&addressdetails=1&accept-language=en&limit={Math.Clamp(limit, 1, 20)}&q={Uri.EscapeDataString(query.Trim())}";

            var response = await _httpClient.GetFromJsonAsync<List<NominatimSearchWithAddressResponse>>(url, cancellationToken);
            if (response is null)
                return Array.Empty<GeocodingResult>();

            var results = new List<GeocodingResult>();
            foreach (var item in response)
            {
                if (!double.TryParse(item.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                    !double.TryParse(item.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
                {
                    continue;
                }

                var city = item.Address?.City ?? item.Address?.Town ?? item.Address?.Village ?? item.Address?.County;
                var country = item.Address?.Country;
                if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country))
                    continue;

                results.Add(new GeocodingResult
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    DisplayName = BuildDisplayName(city, country, item.DisplayName),
                    City = city,
                    Country = country,
                });
            }

            // Nominatim can return the same city multiple times under different OSM feature types.
            return results
                .GroupBy(r => (City: r.City!.ToLowerInvariant(), Country: r.Country!.ToLowerInvariant()))
                .Select(g => g.First())
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "City search failed for {Query}", query);
            return Array.Empty<GeocodingResult>();
        }
    }

    public async Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var key = BuildReverseKey(latitude, longitude);

        var cached = await _context.GeocodingCacheEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entry => entry.Provider == ProviderName && entry.ReverseKey == key,
                cancellationToken);

        if (cached is not null)
            return ToResult(cached);

        try
        {
            await WaitForRateLimitAsync(cancellationToken);

            var lat = latitude.ToString("F6", CultureInfo.InvariantCulture);
            var lon = longitude.ToString("F6", CultureInfo.InvariantCulture);
            var url = $"/reverse?format=jsonv2&lat={lat}&lon={lon}&zoom=10&addressdetails=1";

            var response = await _httpClient.GetFromJsonAsync<NominatimReverseResponse>(url, cancellationToken);
            if (response is null)
                return null;

            var city = response.Address?.City
                ?? response.Address?.Town
                ?? response.Address?.Village
                ?? response.Address?.County;
            var country = response.Address?.Country;
            var displayName = BuildDisplayName(city, country, response.DisplayName);

            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            var result = new GeocodingResult
            {
                Latitude = latitude,
                Longitude = longitude,
                DisplayName = displayName,
                City = city,
                Country = country
            };

            await CacheReverseAsync(key, result, cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Reverse geocoding failed for {Latitude}, {Longitude}", latitude, longitude);
            return null;
        }
    }

    private static async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestAt;
            if (elapsed < TimeSpan.FromSeconds(1))
                await Task.Delay(TimeSpan.FromSeconds(1) - elapsed, cancellationToken);

            _lastRequestAt = DateTimeOffset.UtcNow;
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private async Task CacheForwardAsync(string key, GeocodingResult result, CancellationToken cancellationToken)
    {
        _context.GeocodingCacheEntries.Add(new GeocodingCacheEntry
        {
            Provider = ProviderName,
            ForwardKey = key,
            DisplayName = result.DisplayName,
            City = result.City,
            Country = result.Country,
            Latitude = result.Latitude,
            Longitude = result.Longitude
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(ex, "Forward geocoding cache write skipped for {ForwardKey}", key);
        }
    }

    private async Task CacheReverseAsync(string key, GeocodingResult result, CancellationToken cancellationToken)
    {
        _context.GeocodingCacheEntries.Add(new GeocodingCacheEntry
        {
            Provider = ProviderName,
            ReverseKey = key,
            DisplayName = result.DisplayName,
            City = result.City,
            Country = result.Country,
            Latitude = result.Latitude,
            Longitude = result.Longitude
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(ex, "Reverse geocoding cache write skipped for {ReverseKey}", key);
        }
    }

    private static string BuildForwardKey(string city, string country)
    {
        var normalizedCity = city.Trim().ToLowerInvariant();
        var normalizedCountry = country.Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(normalizedCity) || string.IsNullOrWhiteSpace(normalizedCountry)
            ? string.Empty
            : $"{normalizedCity}|{normalizedCountry}";
    }

    private static string BuildReverseKey(double latitude, double longitude)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{latitude:F6}|{longitude:F6}");
    }

    private static GeocodingResult ToResult(GeocodingCacheEntry entry)
    {
        return new GeocodingResult
        {
            Latitude = entry.Latitude,
            Longitude = entry.Longitude,
            DisplayName = entry.DisplayName,
            City = entry.City,
            Country = entry.Country
        };
    }

    private static string? BuildDisplayName(string? city, string? country, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country))
            return $"{city}, {country}";

        return string.IsNullOrWhiteSpace(fallback)
            ? null
            : fallback;
    }

    private sealed class NominatimSearchResponse
    {
        [JsonPropertyName("lat")]
        public string? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }
    }

    private sealed class NominatimSearchWithAddressResponse
    {
        [JsonPropertyName("lat")]
        public string? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; set; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimReverseResponse
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; set; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("town")]
        public string? Town { get; set; }

        [JsonPropertyName("village")]
        public string? Village { get; set; }

        [JsonPropertyName("county")]
        public string? County { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
