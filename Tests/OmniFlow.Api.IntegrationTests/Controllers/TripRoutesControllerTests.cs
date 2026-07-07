using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Routes;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class TripRoutesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TripRoutesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetRoutes_PublishedTrip_AnonymousReturnsSegments()
    {
        var ownerId = await GetTestUserIdAsync();
        var tripId = await CreateTripWithDestinationsAsync(ownerId, TripStatus.Published);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/routes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<TripRoutesResponse>(response);
        result!.TripId.Should().Be(tripId);
        result.Segments.Should().HaveCount(1);
        result.Segments[0].Driving.Should().NotBeNull();
        result.Segments[0].Driving!.Coordinates.Should().HaveCount(2);
        result.Segments[0].Driving!.Coordinates[0].Should().Equal(2.3522, 48.8566);
    }

    [Fact]
    public async Task GetRoutes_DraftTrip_AnonymousReturns404()
    {
        var ownerId = await GetTestUserIdAsync();
        var tripId = await CreateTripWithDestinationsAsync(ownerId, TripStatus.Draft);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/routes");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoutes_DraftTrip_OwnerReturns200()
    {
        var ownerId = await GetTestUserIdAsync();
        var tripId = await CreateTripWithDestinationsAsync(ownerId, TripStatus.Draft);
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/routes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await DeserializeAsync<TripRoutesResponse>(response);
        result!.Segments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRoutes_NatureTimelineEntry_ChangesSignatureAndRecomputesWalkingProfile()
    {
        var ownerId = await GetTestUserIdAsync();
        var tripId = await CreateTripWithDestinationsAsync(ownerId, TripStatus.Published, sameCity: true);

        var firstResponse = await _client.GetAsync($"/api/v1/trips/{tripId}/routes");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await DeserializeAsync<TripRoutesResponse>(firstResponse);
        firstResult!.Segments[0].Walking!.DurationSeconds.Should().Be(900);

        await AddNatureTimelineEntryAsync(tripId);

        var secondResponse = await _client.GetAsync($"/api/v1/trips/{tripId}/routes");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await DeserializeAsync<TripRoutesResponse>(secondResponse);
        secondResult!.Segments[0].Walking!.DurationSeconds.Should().Be(700);
    }

    [Fact]
    public async Task TripRouteCache_TripHardDelete_Cascades()
    {
        var ownerId = await GetTestUserIdAsync();
        var tripId = await CreateTripWithDestinationsAsync(ownerId, TripStatus.Published);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/routes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        (await db.TripRouteCaches.CountAsync(c => c.TripId == tripId)).Should().Be(1);

        var trip = await db.Trips.IgnoreQueryFilters().FirstAsync(t => t.Id == tripId);
        db.Trips.Remove(trip);
        await db.SaveChangesAsync();

        (await db.TripRouteCaches.IgnoreQueryFilters().CountAsync(c => c.TripId == tripId)).Should().Be(0);
    }

    private async Task<Guid> GetTestUserIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        return await db.Users
            .Where(u => u.Email == TestDatabaseSeeder.TestUserEmail)
            .Select(u => u.Id)
            .FirstAsync();
    }

    private async Task<Guid> CreateTripWithDestinationsAsync(Guid ownerId, TripStatus status, bool sameCity = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = $"Route Trip {Guid.NewGuid():N}",
            Status = status,
            Origin = "Paris",
            OriginCountry = "France",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelCompanion = TravelCompanion.Couple,
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.PublicTransport,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
        };

        var first = new TripDestination(new DateOnly(2030, 1, 10), new DateOnly(2030, 1, 12), "Paris", "France", 1)
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id
        };
        first.SetCoordinates(48.8566, 2.3522);

        var second = new TripDestination(new DateOnly(2030, 1, 12), new DateOnly(2030, 1, 15), sameCity ? "Paris" : "Lyon", "France", 2)
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id
        };
        second.SetCoordinates(sameCity ? 48.8584 : 45.7640, sameCity ? 2.2945 : 4.8357);

        trip.Destinations.Add(first);
        trip.Destinations.Add(second);
        trip.RecalculateFromDestinations();

        db.Trips.Add(trip);
        db.TripDestinations.AddRange(first, second);
        await db.SaveChangesAsync();
        return trip.Id;
    }

    private async Task AddNatureTimelineEntryAsync(Guid tripId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var destinationId = await db.TripDestinations
            .Where(d => d.TripId == tripId)
            .OrderBy(d => d.OrderIndex)
            .Select(d => d.Id)
            .FirstAsync();

        var place = new Place
        {
            Id = Guid.NewGuid(),
            Name = "Forest Path",
            Category = PlaceCategory.Forest,
            City = "Paris",
            Country = "France",
            Latitude = 48.857,
            Longitude = 2.35
        };

        db.Places.Add(place);
        db.TimelineEntries.Add(TimelineEntry.CreatePlaceEntry(tripId, destinationId, 1, 100, place.Id));
        await db.SaveChangesAsync();
    }

    private async Task<string> GetAccessTokenAsync(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, JsonOptions);
        return result!.AccessToken!;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
