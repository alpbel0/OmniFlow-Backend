using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

/// <summary>
/// Integration tests for Task 5.5: Trip Save/Unsave & Upvote endpoints.
/// Note: Tests that require published trips are marked as Skip because
/// trips cannot be published without stops (no Stops API available yet).
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
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(createBody);
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

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Upvote_SelfUpvote_Returns409()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Upvote_WithValidToken_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Upvote_Duplicate_Returns409()
    {
        // This test requires a published trip, which requires stops
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

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task RemoveUpvote_WithValidToken_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task RemoveUpvote_NotUpvoted_Returns404()
    {
        // This test requires a published trip, which requires stops
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

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Save_SelfSave_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Save_WithValidToken_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Save_Duplicate_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    // ── DELETE Save Tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Unsave_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}/save");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Unsave_WithValidToken_Returns204()
    {
        // This test requires a published trip, which requires stops
    }

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task Unsave_NotSaved_Returns404()
    {
        // This test requires a published trip, which requires stops
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

    [Fact(Skip = "Cannot publish trip without stops - Stops API not available")]
    public async Task FullFlow_Save_GetSavedTrips_Unsave()
    {
        // This test requires a published trip, which requires stops
    }
}