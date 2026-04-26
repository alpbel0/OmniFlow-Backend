using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for Task 5.5: Trip Save/Unsave & Upvote endpoints.
/// </summary>
[Collection("Integration")]
public class TripsControllerSaveUpvoteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TripsControllerSaveUpvoteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    // ── Helper Methods ─────────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(string email, string password)
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

    private async Task<Guid> CreateTripAsync(HttpClient authClient)
    {
        var createRequest = new CreateTripRequest
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(createBody);
    }

    /// <summary>
    /// Creates a trip, adds a stop, and publishes it.
    /// Returns the published trip ID.
    /// </summary>
    private async Task<Guid> CreatePublishedTripAsync(HttpClient authClient)
    {
        // 1. Create trip
        var tripId = await CreateTripAsync(authClient);

        // 2. Add a timeline entry (required for publishing)
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dest = new TripDestination(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "TestCity", "TestCountry", 1)
        {
            TripId = tripId
        };
        await db.TripDestinations.AddAsync(dest);
        await db.SaveChangesAsync();

        var entry = TimelineEntry.CreateCustomEventEntry(tripId, dest.Id, 1, 1000, "Test Event", new TimeOnly(10, 0), 60);
        await db.TimelineEntries.AddAsync(entry);
        await db.SaveChangesAsync();

        // 3. Publish trip
        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return tripId;
    }

    /// <summary>
    /// Creates a trip owned by the test user, adds a stop, and publishes it.
    /// Uses a second user (admin) to create it, so the test user can interact with it.
    /// </summary>
    private async Task<Guid> CreatePublishedTripForOtherUserAsync(HttpClient authClient)
    {
        // Use admin credentials to create a trip owned by someone else
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        return await CreatePublishedTripAsync(adminClient);
    }

    // ── POST Upvote Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Upvote_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/upvote", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upvote_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/upvote", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Upvote_DraftTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create a draft trip
        var tripId = await CreateTripAsync(authClient);

        // Try to upvote draft trip
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upvote_SelfUpvote_Returns409()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create and publish trip owned by test user
        var tripId = await CreatePublishedTripAsync(authClient);

        // Try to upvote own trip
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Upvote_WithValidToken_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user (admin)
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // Upvote the trip
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Upvote_Duplicate_Returns409()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // First upvote
        var firstResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Try duplicate upvote
        var secondResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE Upvote Tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveUpvote_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}/upvote");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveUpvote_TripNotFound_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}/upvote");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveUpvote_WithValidToken_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // First upvote
        await authClient.PostAsync($"/api/v1/trips/{tripId}/upvote", null);

        // Remove upvote
        var response = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/upvote");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveUpvote_NotUpvoted_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user (but don't upvote)
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // Try to remove upvote that doesn't exist
        var response = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/upvote");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST Save Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/save", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Save_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/save", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Save_DraftTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var tripId = await CreateTripAsync(authClient);

        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Save_SelfSave_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create and publish trip owned by test user
        var tripId = await CreatePublishedTripAsync(authClient);

        // Save own trip (allowed)
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Save_WithValidToken_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // Save the trip
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Save_Duplicate_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // First save
        var firstResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Duplicate save (idempotent, returns 204)
        var secondResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── DELETE Save Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Unsave_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}/save");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unsave_WithValidToken_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // First save
        await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);

        // Unsave
        var response = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/save");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Unsave_NotSaved_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user (but don't save)
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // Try to unsave a trip that wasn't saved
        var response = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/save");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET Saved Trips Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetSavedTrips_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/saved-trips");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSavedTrips_WithValidToken_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/v1/saved-trips");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResponse<SavedTripResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetSavedTrips_ReturnsCorrectPagination()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/v1/saved-trips?pageNumber=2&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResponse<SavedTripResponse>>(body, _json);

        result!.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task FullFlow_Save_GetSavedTrips_Unsave()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by another user
        var tripId = await CreatePublishedTripForOtherUserAsync(authClient);

        // Save the trip
        var saveResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/save", null);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Get saved trips
        var getResponse = await authClient.GetAsync("/api/v1/saved-trips");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var savedTrips = JsonSerializer.Deserialize<PagedResponse<SavedTripResponse>>(getBody, _json);

        savedTrips!.Data.Should().HaveCountGreaterThan(0);
        savedTrips.Data.First().TripId.Should().Be(tripId);

        // Unsave the trip
        var unsaveResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/save");
        unsaveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify removed from saved trips
        var getAfterResponse = await authClient.GetAsync("/api/v1/saved-trips");
        var getAfterBody = await getAfterResponse.Content.ReadAsStringAsync();
        var savedTripsAfter = JsonSerializer.Deserialize<PagedResponse<SavedTripResponse>>(getAfterBody, _json);

        // Verify the trip is no longer in saved list
        savedTripsAfter!.Data.Should().NotContain(t => t.TripId == tripId);
    }
}