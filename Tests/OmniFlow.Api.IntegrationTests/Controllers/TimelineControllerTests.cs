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
        var entry = TimelineEntry.CreateCustomEventEntry(tripId, destinationId, 1, 1000.0, "Event", new TimeOnly(10, 0), 60);
        await db.TimelineEntries.AddAsync(entry);
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
            PlaceId = Guid.NewGuid()
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TimelineEntryResponse>(body, _json);
        return result!.Id;
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
    public async Task GetTimeline_DraftTrip_OtherUser_Returns403()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.GetAsync($"/api/v1/trips/{tripId}/timeline");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
    public async Task CreatePlaceEntry_OwnerDraft_Returns201()
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
        result.IsLocked.Should().BeTrue();
        result.BufferMinutes.Should().Be(0);
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
    public async Task UpdateUnlockedEntry_AllFields_Returns200()
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

        var updateRequest = new UpdateTimelineEntryRequest
        {
            Id = entryId,
            DestinationId = destinationId,
            DayNumber = 1,
            CustomName = "Updated Event",
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
            Price = 250
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/timeline/entry/{entryId}", updateRequest);
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