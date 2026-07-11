using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.DTOs.TripDestinations;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class TimelineControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TimelineControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        TestDatabaseSeeder.SeedProviderDataAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    // ── Helper Methods ─────────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(string email = TestDatabaseSeeder.TestUserEmail, string password = TestDatabaseSeeder.TestUserPassword)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new OmniFlow.Application.DTOs.Account.AuthenticationRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await loginResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<OmniFlow.Application.DTOs.Account.AuthenticationResponse>(body, _json);
        return result!.AccessToken!;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(Guid TripId, Guid DestinationId)> CreateDraftTripWithDestinationAsync(HttpClient authClient)
    {
        var createRequest = new CreateTripRequest
        {
            Title = "Draft Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 8, 10),
            EndDate = new DateOnly(2026, 8, 13),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(createBody, _json);
        var tripId = createResult!.TripId;

        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(getBody, _json);
        var destinationId = destinations!.First().Id;

        return (tripId, destinationId);
    }

    private async Task<(Guid TripId, Guid DestinationId)> CreatePublishedTripWithDestinationAsync(HttpClient authClient)
    {
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var trip = db.Trips.First(t => t.Id == tripId);
        trip.Description = "Publishable trip for timeline tests";
        trip.CoverPhotoUrl = "https://example.com/cover.jpg";
        trip.EstimatedCost = 1200;
        var firstEntry = TimelineEntry.CreateCustomEventEntry(tripId, destinationId, 1, 1000.0, "Event", new TimeOnly(10, 0), 60);
        var secondEntry = TimelineEntry.CreateCustomEventEntry(tripId, destinationId, 1, 1001.0, "Dinner", new TimeOnly(19, 0), 60);
        await db.TimelineEntries.AddRangeAsync(firstEntry, secondEntry);
        await db.SaveChangesAsync();

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return (tripId, destinationId);
    }

    private async Task<Guid> AddPlaceEntryAsync(HttpClient authClient, Guid tripId, Guid destinationId)
    {
        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.Place,
            PlaceId = GetExistingPlaceId()
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        return result!.Id;
    }

    private Guid GetExistingPlaceId()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        return db.Places
            .Select(p => p.Id)
            .First();
    }

    // ── GET Timeline Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTimeline_PublishedTrip_NoToken_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(authClient);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTimeline_PublishedTrip_Owner_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTimeline_DraftTrip_Owner_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTimeline_DraftTrip_OtherUser_Returns404()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTimeline_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/trips/{Guid.NewGuid()}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTimeline_WithDestinationFilter_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreatePublishedTripWithDestinationAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline?destinationId={destinationId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Create Entry Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCustomEventEntry_WithCoordinates_Returns201AndGetTimelineIncludesCoordinates()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Test Event",
            CustomLatitude = 41.0082,
            CustomLongitude = 28.9784,
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60,
            Price = 50
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomEvent);
        result.CustomName.Should().Be("Test Event");
        result.CustomLatitude.Should().Be(41.0082);
        result.CustomLongitude.Should().Be(28.9784);
        result.IsLocked.Should().BeTrue();
        result.BufferMinutes.Should().Be(0);
        result.PlanningSlotKey.Should().BeNull();

        var timelineResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timelineBody = await timelineResponse.Content.ReadAsStringAsync();
        var timeline = JsonSerializer.Deserialize<List<TimelineEntryResponse>>(timelineBody, _json);
        var timelineEntry = timeline.Should().ContainSingle(e => e.Id == result.Id).Subject;
        timelineEntry.CustomLatitude.Should().Be(41.0082);
        timelineEntry.CustomLongitude.Should().Be(28.9784);
    }

    [Fact]
    public async Task CreateCustomTransportEntry_WithFromToCoordinates_Returns201AndGetTimelineIncludesCoordinates()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomTransport,
            TransportType = TransportMode.Train,
            TransportFromStation = "Roma Termini",
            TransportToStation = "Firenze SMN",
            TransportCompany = "Trenitalia",
            TransportFromLatitude = 41.901,
            TransportFromLongitude = 12.501,
            TransportToLatitude = 43.776,
            TransportToLongitude = 11.248,
            StartTime = new TimeOnly(9, 0),
            DurationMinutes = 90,
            Price = 55
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(await response.Content.ReadAsStringAsync(), _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomTransport);
        result.TransportFromLatitude.Should().Be(41.901);
        result.TransportFromLongitude.Should().Be(12.501);
        result.TransportToLatitude.Should().Be(43.776);
        result.TransportToLongitude.Should().Be(11.248);

        var timelineResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = JsonSerializer.Deserialize<List<TimelineEntryResponse>>(
            await timelineResponse.Content.ReadAsStringAsync(),
            _json);
        var timelineEntry = timeline.Should().ContainSingle(e => e.Id == result.Id).Subject;
        timelineEntry.TransportFromLatitude.Should().Be(41.901);
        timelineEntry.TransportFromLongitude.Should().Be(12.501);
        timelineEntry.TransportToLatitude.Should().Be(43.776);
        timelineEntry.TransportToLongitude.Should().Be(11.248);
    }

    [Fact]
    public async Task CreateCustomEventEntry_WithIsLockedFalse_ReturnsUnlockedEntry()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Map Place",
            StartTime = new TimeOnly(14, 0),
            DurationMinutes = 60,
            IsLocked = false
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomEvent);
        result.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCustomEventEntry_WithoutIsLocked_ReturnsLockedEntry()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Reserved Event",
            StartTime = new TimeOnly(16, 0),
            DurationMinutes = 60
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEntry_WithPlanningSlotKey_ReturnsKeyAndGetTimelineIncludesKey()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);
        var planningSlotKey = $"hotel-night:{destinationId:D}:1";

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.Place,
            PlaceId = GetExistingPlaceId(),
            PlanningSlotKey = $"  {planningSlotKey.ToUpperInvariant()}  "
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.PlanningSlotKey.Should().Be(planningSlotKey);

        var timelineResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        timelineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var timeline = JsonSerializer.Deserialize<List<TimelineEntryResponse>>(
            await timelineResponse.Content.ReadAsStringAsync(),
            _json);
        timeline.Should().ContainSingle(e => e.PlanningSlotKey == planningSlotKey);
    }

    [Fact]
    public async Task CreateEntry_DuplicateActivePlanningSlotKey_Returns409()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);
        var planningSlotKey = $"hotel-night:{destinationId:D}:1";

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.Place,
            PlaceId = GetExistingPlaceId(),
            PlanningSlotKey = planningSlotKey
        };

        var firstResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicateResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateEntry_StalePlanningSlotKey_Returns404()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.Place,
            PlaceId = GetExistingPlaceId(),
            PlanningSlotKey = $"hotel-night:{Guid.NewGuid():D}:1"
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateEntry_SoftDeletedPlanningSlotKey_CanBeReused()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);
        var planningSlotKey = $"hotel-night:{destinationId:D}:1";

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.Place,
            PlaceId = GetExistingPlaceId(),
            PlanningSlotKey = planningSlotKey
        };

        var firstResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstResult = JsonSerializer.Deserialize<TimelineEntryResponse>(
            await firstResponse.Content.ReadAsStringAsync(),
            _json);

        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/timeline/entry/{firstResult!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCustomFlightEntry_OwnerDraft_Returns201()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomFlight,
            FlightFromAirport = "IST",
            FlightToAirport = "FCO",
            FlightDepartureAt = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc),
            FlightArrivalAt = new DateTime(2026, 8, 10, 10, 30, 0, DateTimeKind.Utc),
            Price = 200
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomFlight);
        result.IsLocked.Should().BeTrue();
        result.BufferMinutes.Should().Be(120);
    }

    [Fact]
    public async Task CreateProviderFlightEntry_OwnerDraft_Returns201WithProviderReference()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);
        var providerFlightId = Guid.Parse("a1111111-1111-1111-1111-111111111111");

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomFlight,
            ProviderFlightId = providerFlightId
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomFlight);
        result.ProviderFlightId.Should().Be(providerFlightId);
        result.FlightFromAirport.Should().Be("IST");
        result.FlightToAirport.Should().Be("CDG");
        result.FlightFromCity.Should().Be("Istanbul");
        result.FlightToCity.Should().Be("Paris");
        result.Price.Should().Be(200);
        result.CurrencyCode.Should().Be("USD");
        result.IsLocked.Should().BeTrue();
        result.BufferMinutes.Should().Be(120);
    }

    [Fact]
    public async Task CreateProviderHotelEntry_OwnerDraft_Returns201WithProviderReference()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);
        var providerHotelId = Guid.Parse("b1111111-1111-1111-1111-111111111111");

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomAccommodation,
            ProviderHotelId = providerHotelId,
            IsLocked = false
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        result!.EntryType.Should().Be(TimelineEntryType.CustomAccommodation);
        result.ProviderHotelId.Should().Be(providerHotelId);
        result.CustomName.Should().Be("Budget Paris Inn");
        result.AccommodationCheckIn.Should().Be(new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc));
        result.AccommodationCheckOut.Should().Be(new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc));
        result.Price.Should().Be(240);
        result.CurrencyCode.Should().Be("USD");
        result.IsLocked.Should().BeTrue();
        result.BufferMinutes.Should().Be(0);
    }

    [Fact]
    public async Task CreateEntry_WithoutToken_Returns401()
    {
        var request = new CreateTimelineEntryRequest
        {
            TripId = Guid.NewGuid(),
            DestinationId = Guid.NewGuid(),
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Test"
        };

        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEntry_PublishedTrip_Returns400()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreatePublishedTripWithDestinationAsync(authClient);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Test Event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEntry_OtherUser_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var request = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Test Event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var response = await otherClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Update Entry Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEntry_CommonFields_OnUnlockedEntry_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Original Event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60,
            Price = 50
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        // Unlock the entry so type-specific fields can also be updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var entry = db.TimelineEntries.First(e => e.Id == entryId);
            entry.Unlock();
            await db.SaveChangesAsync();
        }

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            CustomName = "Updated Event",
            CustomLatitude = 40.7128,
            CustomLongitude = -74.0060,
            StartTime = new TimeOnly(11, 0),
            DurationMinutes = 90,
            Price = 75,
            CurrencyCode = "EUR"
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        var updateResult = JsonSerializer.Deserialize<TimelineEntryResponse>(updateBody, _json);
        updateResult!.CustomName.Should().Be("Updated Event");
        updateResult.CustomLatitude.Should().Be(40.7128);
        updateResult.CustomLongitude.Should().Be(-74.0060);
        updateResult.Price.Should().Be(75);
    }

    [Fact]
    public async Task UpdateLockedEntry_TypeSpecificChange_Returns400()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomFlight,
            FlightFromAirport = "IST",
            FlightToAirport = "FCO",
            FlightDepartureAt = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc),
            FlightArrivalAt = new DateTime(2026, 8, 10, 10, 30, 0, DateTimeKind.Utc),
            Price = 200
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            FlightFromAirport = "SAW",
            FlightToAirport = "CIA",
            FlightDepartureAt = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc),
            FlightArrivalAt = new DateTime(2026, 8, 11, 11, 30, 0, DateTimeKind.Utc),
            Price = 250
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCustomTransportEntry_WhenUnlocked_UpdatesFromToCoordinates()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomTransport,
            TransportType = TransportMode.Train,
            TransportFromStation = "Roma Termini",
            TransportToStation = "Firenze SMN",
            TransportFromLatitude = 41.901,
            TransportFromLongitude = 12.501,
            TransportToLatitude = 43.776,
            TransportToLongitude = 11.248,
            StartTime = new TimeOnly(9, 0),
            DurationMinutes = 90
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(
            await createResponse.Content.ReadAsStringAsync(),
            _json);
        var entryId = createResult!.Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var entry = db.TimelineEntries.First(e => e.Id == entryId);
            entry.Unlock();
            await db.SaveChangesAsync();
        }

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            TransportType = TransportMode.Train,
            TransportFromStation = "Roma Tiburtina",
            TransportToStation = "Bologna Centrale",
            TransportFromLatitude = 41.911,
            TransportFromLongitude = 12.530,
            TransportToLatitude = 44.505,
            TransportToLongitude = 11.343,
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 120
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResult = JsonSerializer.Deserialize<TimelineEntryResponse>(
            await updateResponse.Content.ReadAsStringAsync(),
            _json);
        updateResult!.TransportFromLatitude.Should().Be(41.911);
        updateResult.TransportFromLongitude.Should().Be(12.530);
        updateResult.TransportToLatitude.Should().Be(44.505);
        updateResult.TransportToLongitude.Should().Be(11.343);
    }

    [Fact]
    public async Task UpdateCustomTransportEntry_WhenLockedCoordinateChanges_Returns400()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomTransport,
            TransportType = TransportMode.Train,
            TransportFromStation = "Roma Termini",
            TransportToStation = "Firenze SMN",
            TransportFromLatitude = 41.901,
            TransportFromLongitude = 12.501,
            TransportToLatitude = 43.776,
            TransportToLongitude = 11.248
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(
            await createResponse.Content.ReadAsStringAsync(),
            _json);

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = createResult!.Id,
            DestinationId = destinationId,
            DayNumber = 1,
            TransportFromLatitude = 41.911
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{createResult.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateLockedEntry_CommonFields_Returns200()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomFlight,
            FlightFromAirport = "IST",
            FlightToAirport = "FCO",
            FlightDepartureAt = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc),
            FlightArrivalAt = new DateTime(2026, 8, 10, 10, 30, 0, DateTimeKind.Utc),
            Price = 200
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            Price = 350,
            CurrencyCode = "EUR",
            Notes = "Updated price"
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateEntry_OtherUser_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            Price = 100
        };

        var updateResponse = await otherClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateEntry_PublishedTrip_Returns400()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreatePublishedTripWithDestinationAsync(authClient);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var entries = await db.TimelineEntries.ToListAsync();
        var entry = entries.First(e => e.TripId == tripId);

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entry.Id,
            DestinationId = destinationId,
            DayNumber = 1,
            Price = 999
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entry.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Delete Entry Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUnlockedEntry_OwnerDraft_Returns204()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Event to delete",
            StartTime = new TimeOnly(14, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        // Unlock the entry so it can be deleted
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var entry = db.TimelineEntries.First(e => e.Id == entryId);
            entry.Unlock();
            await db.SaveChangesAsync();
        }

        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteLockedEntry_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomFlight,
            FlightFromAirport = "IST",
            FlightToAirport = "FCO",
            FlightDepartureAt = new DateTime(2026, 8, 10, 8, 0, 0, DateTimeKind.Utc),
            FlightArrivalAt = new DateTime(2026, 8, 10, 10, 30, 0, DateTimeKind.Utc),
            Price = 200
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteEntry_OtherUser_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json);
        var entryId = createResult!.Id;

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var deleteResponse = await otherClient.DeleteAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Reorder Entry Tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderEntry_BetweenTwoEntries_Returns204()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var req1 = new CreateTimelineEntryRequest
        {
            TripId = tripId, DestinationId = destinationId, DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent, CustomName = "First",
            StartTime = new TimeOnly(9, 0), DurationMinutes = 60
        };
        var req2 = new CreateTimelineEntryRequest
        {
            TripId = tripId, DestinationId = destinationId, DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent, CustomName = "Second",
            StartTime = new TimeOnly(11, 0), DurationMinutes = 60
        };
        var req3 = new CreateTimelineEntryRequest
        {
            TripId = tripId, DestinationId = destinationId, DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent, CustomName = "Third",
            StartTime = new TimeOnly(13, 0), DurationMinutes = 60
        };

        var res1 = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", req1);
        var res2 = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", req2);
        var res3 = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", req3);

        var entry1Id = JsonSerializer.Deserialize<TimelineEntryResponse>(await res1.Content.ReadAsStringAsync(), _json)!.Id;
        var entry3Id = JsonSerializer.Deserialize<TimelineEntryResponse>(await res3.Content.ReadAsStringAsync(), _json)!.Id;
        var entry2Id = JsonSerializer.Deserialize<TimelineEntryResponse>(await res2.Content.ReadAsStringAsync(), _json)!.Id;

        var reorderRequest = new ReorderTimelineEntriesRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            EntryId = entry2Id,
            BeforeEntryId = entry3Id,
            AfterEntryId = entry1Id
        };

        var reorderResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/reorder", reorderRequest);
        reorderResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Mark Visited Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkVisited_Owner_Returns204()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Visit me",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var entryId = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json)!.Id;

        var visitedRequest = new { isVisited = true };
        var visitedResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}/visited", visitedRequest);
        visitedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkUnvisited_Owner_Returns204()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "Unvisit me",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var entryId = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json)!.Id;

        var visitedRequest2 = new { isVisited = true };
        await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}/visited", visitedRequest2);

        var unvisitedRequest = new { isVisited = false };
        var unvisitedResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}/visited", unvisitedRequest);
        unvisitedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkVisited_OtherUser_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        var createRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomEvent,
            CustomName = "My event",
            StartTime = new TimeOnly(10, 0),
            DurationMinutes = 60
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var entryId = JsonSerializer.Deserialize<TimelineEntryResponse>(createBody, _json)!.Id;

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var visitedRequest3 = new { isVisited = true };
        var visitedResponse = await otherClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}/visited", visitedRequest3);
        visitedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
