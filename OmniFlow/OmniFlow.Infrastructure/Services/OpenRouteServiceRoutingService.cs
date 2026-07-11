using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniFlow.Application.DTOs.Routes;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Settings;

namespace OmniFlow.Infrastructure.Services;

public class OpenRouteServiceRoutingService : IRoutingService
{
    private static readonly SemaphoreSlim RequestGate = new(3, 3);

    private readonly HttpClient _httpClient;
    private readonly OpenRouteServiceSettings _settings;
    private readonly ILogger<OpenRouteServiceRoutingService> _logger;

    public OpenRouteServiceRoutingService(
        HttpClient httpClient,
        IOptions<OpenRouteServiceSettings> settings,
        ILogger<OpenRouteServiceRoutingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<RouteDetailDto> GetRouteAsync(
        string profile,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning(
                "ORS route request skipped for profile {Profile}: Routing:OpenRouteService:ApiKey is not configured",
                profile);
            return RouteDetailDto.Empty();
        }

        await RequestGate.WaitAsync(cancellationToken);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/v2/directions/{profile}/geojson");
            request.Headers.TryAddWithoutValidation("Authorization", _settings.ApiKey);
            request.Content = JsonContent.Create(new OrsDirectionsRequest
            {
                Coordinates =
                [
                    [fromLongitude, fromLatitude],
                    [toLongitude, toLatitude]
                ]
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ORS route request failed with status {StatusCode} for profile {Profile}",
                    (int)response.StatusCode,
                    profile);
                return RouteDetailDto.Empty();
            }

            var payload = await response.Content.ReadFromJsonAsync<OrsDirectionsResponse>(cancellationToken);
            var feature = payload?.Features?.FirstOrDefault();
            if (feature?.Geometry?.Coordinates is null || feature.Properties?.Summary is null)
                return RouteDetailDto.Empty();

            return new RouteDetailDto
            {
                Coordinates = feature.Geometry.Coordinates
                    .Where(c => c.Count >= 2)
                    .Select(c => new List<double> { c[0], c[1] })
                    .ToList(),
                DistanceMeters = feature.Properties.Summary.Distance,
                DurationSeconds = feature.Properties.Summary.Duration
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "ORS route request failed for profile {Profile}", profile);
            return RouteDetailDto.Empty();
        }
        finally
        {
            RequestGate.Release();
        }
    }

    private sealed class OrsDirectionsRequest
    {
        [JsonPropertyName("coordinates")]
        public List<List<double>> Coordinates { get; set; } = new();
    }

    private sealed class OrsDirectionsResponse
    {
        [JsonPropertyName("features")]
        public List<OrsFeature>? Features { get; set; }
    }

    private sealed class OrsFeature
    {
        [JsonPropertyName("geometry")]
        public OrsGeometry? Geometry { get; set; }

        [JsonPropertyName("properties")]
        public OrsProperties? Properties { get; set; }
    }

    private sealed class OrsGeometry
    {
        [JsonPropertyName("coordinates")]
        public List<List<double>>? Coordinates { get; set; }
    }

    private sealed class OrsProperties
    {
        [JsonPropertyName("summary")]
        public OrsSummary? Summary { get; set; }
    }

    private sealed class OrsSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }
}
