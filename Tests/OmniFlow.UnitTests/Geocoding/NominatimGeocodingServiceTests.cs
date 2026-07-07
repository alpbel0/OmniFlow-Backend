using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Services;
using OmniFlow.Infrastructure.Settings;

namespace OmniFlow.UnitTests.Geocoding;

public class NominatimGeocodingServiceTests
{
    [Fact]
    public async Task GeocodeAsync_WhenForwardCacheHit_DoesNotCallHttpClient()
    {
        var cacheEntries = new List<GeocodingCacheEntry>
        {
            new()
            {
                Provider = "nominatim",
                ForwardKey = "paris|france",
                DisplayName = "Paris, France",
                City = "Paris",
                Country = "France",
                Latitude = 48.8566,
                Longitude = 2.3522
            }
        };
        var cacheSet = MockDbSetHelper.CreateAsyncMockDbSet(cacheEntries);
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.GeocodingCacheEntries).Returns(cacheSet.Object);
        var handler = new CountingHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test") };
        var service = CreateService(httpClient, contextMock.Object);

        var result = await service.GeocodeAsync("Paris", "France", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Latitude.Should().Be(48.8566);
        result.Longitude.Should().Be(2.3522);
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_WhenReverseCacheHit_DoesNotCallHttpClient()
    {
        var cacheEntries = new List<GeocodingCacheEntry>
        {
            new()
            {
                Provider = "nominatim",
                ReverseKey = "41.008200|28.978400",
                DisplayName = "Istanbul, Turkiye",
                City = "Istanbul",
                Country = "Turkiye",
                Latitude = 41.0082,
                Longitude = 28.9784
            }
        };
        var cacheSet = MockDbSetHelper.CreateAsyncMockDbSet(cacheEntries);
        var contextMock = new Mock<IApplicationDbContext>();
        contextMock.Setup(x => x.GeocodingCacheEntries).Returns(cacheSet.Object);
        var handler = new CountingHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://nominatim.test") };
        var service = CreateService(httpClient, contextMock.Object);

        var result = await service.ReverseGeocodeAsync(41.0082, 28.9784, CancellationToken.None);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Istanbul, Turkiye");
        handler.CallCount.Should().Be(0);
    }

    private static NominatimGeocodingService CreateService(
        HttpClient httpClient,
        IApplicationDbContext context)
    {
        return new NominatimGeocodingService(
            httpClient,
            context,
            Options.Create(new GeocodingSettings
            {
                BaseUrl = "https://nominatim.test",
                UserAgent = "OmniFlow/1.0 (+omniflowinc@gmail.com)",
                TimeoutSeconds = 5
            }),
            NullLogger<NominatimGeocodingService>.Instance);
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });
        }
    }
}
