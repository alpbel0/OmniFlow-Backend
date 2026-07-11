using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmniFlow.Infrastructure.Services;
using OmniFlow.Infrastructure.Settings;

namespace OmniFlow.UnitTests.Routing;

public class OpenRouteServiceRoutingServiceTests
{
    [Fact]
    public async Task GetRouteAsync_WhenApiKeyMissing_DoesNotCallHttpClientAndReturnsEmptyRoute()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler, apiKey: string.Empty);

        var result = await service.GetRouteAsync("driving-car", 48.8566, 2.3522, 45.7640, 4.8357);

        result.Coordinates.Should().BeEmpty();
        result.DistanceMeters.Should().Be(0);
        result.DurationSeconds.Should().Be(0);
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRouteAsync_WhenOrsReturns429_ReturnsEmptyRoute()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.TooManyRequests, "{}");
        var service = CreateService(handler);

        var result = await service.GetRouteAsync("driving-car", 48.8566, 2.3522, 45.7640, 4.8357);

        result.Coordinates.Should().BeEmpty();
        result.DistanceMeters.Should().Be(0);
        result.DurationSeconds.Should().Be(0);
        handler.CallCount.Should().Be(4);
    }

    [Fact]
    public async Task GetRouteAsync_WhenOrsReturnsGeoJson_MapsCoordinatesDistanceAndDuration()
    {
        const string json = """
        {
          "features": [
            {
              "geometry": {
                "coordinates": [[2.3522, 48.8566], [4.8357, 45.7640]]
              },
              "properties": {
                "summary": {
                  "distance": 465000,
                  "duration": 17100
                }
              }
            }
          ]
        }
        """;
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler);

        var result = await service.GetRouteAsync("driving-car", 48.8566, 2.3522, 45.7640, 4.8357);

        result.Coordinates.Should().HaveCount(2);
        result.Coordinates[0].Should().Equal(2.3522, 48.8566);
        result.DistanceMeters.Should().Be(465000);
        result.DurationSeconds.Should().Be(17100);
        handler.LastRequestUri!.AbsolutePath.Should().Be("/v2/directions/driving-car/geojson");
    }

    private static OpenRouteServiceRoutingService CreateService(
        StubHttpMessageHandler handler,
        string apiKey = "test-key")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://ors.test")
        };

        return new OpenRouteServiceRoutingService(
            httpClient,
            Options.Create(new OpenRouteServiceSettings
            {
                BaseUrl = "https://ors.test",
                ApiKey = apiKey,
                TimeoutSeconds = 8
            }),
            NullLogger<OpenRouteServiceRoutingService>.Instance);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body = body;
        }

        public int CallCount { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestUri = request.RequestUri;

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            };

            if (_statusCode == HttpStatusCode.TooManyRequests)
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

            return Task.FromResult(response);
        }
    }
}
