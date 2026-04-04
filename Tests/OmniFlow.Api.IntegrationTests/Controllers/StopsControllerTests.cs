using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Stops;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class StopsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StopsControllerTests(CustomWebApplicationFactory factory)
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

    private async Task<Guid> CreateTestTripAsync(HttpClient client)
    {
        var request = new OmniFlow.Application.DTOs.Trips.CreateTripRequest
        {
            Title = "Test Trip for Stops",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var response = await client.PostAsJsonAsync("/api/v1/trips", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(body);
    }

    // ── GET Stops ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStops_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}/stops");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStops_WithValidToken_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/stops");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var stops = JsonSerializer.Deserialize<List<StopResponse>>(body, _json);

        stops.Should().NotBeNull();
        stops!.Should().BeEmpty(); // No stops yet
    }

    [Fact]
    public async Task GetStops_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/trips/{Guid.NewGuid()}/stops");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST Create Stop ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateStop_WithoutToken_Returns401()
    {
        var request = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Custom Stop",
            CustomCategory = PlaceCategory.Restaurant
        };

        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/stops", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateStop_WithValidToken_Returns201()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        var request = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Test Restaurant",
            CustomCategory = PlaceCategory.Restaurant,
            DurationMinutes = 60,
            ActivityPrice = 100,
            TransportPrice = 20,
            CurrencyCode = "USD"
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var stopId = JsonSerializer.Deserialize<Guid>(body);

        stopId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateStop_InvalidData_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        var request = new CreateStopRequest
        {
            DayNumber = 0, // Invalid: must be > 0
            CustomName = "Test"
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateStop_TimeLockedWithoutArrivalTime_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        var request = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Test Stop",
            CustomCategory = PlaceCategory.Museum,
            IsTimeLocked = true
            // Missing ArrivalTime
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PUT Update Stop ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStop_WithoutToken_Returns401()
    {
        var request = new UpdateStopRequest { Notes = "Updated notes" };

        var response = await _client.PutAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/stops/{Guid.NewGuid()}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStop_Owner_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        // Create stop first
        var createRequest = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Original Name",
            CustomCategory = PlaceCategory.Restaurant
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", createRequest);
        var stopId = JsonSerializer.Deserialize<Guid>(await createResponse.Content.ReadAsStringAsync());

        // Update stop
        var updateRequest = new UpdateStopRequest
        {
            CustomName = "Updated Name",
            Notes = "Updated notes"
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/stops/{stopId}`, updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── DELETE Stop ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteStop_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}/stops/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT Reorder Stops ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderStops_WithoutToken_Returns401()
    {
        var request = new List<ReorderStopRequest>
        {
            new() { StopId = Guid.NewGuid(), NewDayNumber = 1 }
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/stops/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST Mark Visited ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkVisited_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/stops/{Guid.NewGuid()}/visited", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkVisited_Owner_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        // Create stop first
        var createRequest = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Test Stop",
            CustomCategory = PlaceCategory.Museum
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", createRequest);
        var stopId = JsonSerializer.Deserialize<Guid>(await createResponse.Content.ReadAsStringAsync());

        // Mark visited
        var response = await authClient.PostAsync($"/api/v1/trips/{tripId}/stops/{stopId}/visited", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Full Flow Test ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Create_Get_Update_Delete_Stop()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTestTripAsync(authClient);

        // Create stop
        var createRequest = new CreateStopRequest
        {
            DayNumber = 1,
            CustomName = "Full Flow Stop",
            CustomCategory = PlaceCategory.Restaurant,
            DurationMinutes = 90,
            ActivityPrice = 150
        };

        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var stopId = JsonSerializer.Deserialize<Guid>(await createResponse.Content.ReadAsStringAsync());

        // Get stops
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/stops");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var stops = JsonSerializer.Deserialize<List<StopResponse>>(getBody, _json);

        stops!.Count.Should().Be(1);
        stops[0].CustomName.Should().Be("Full Flow Stop");
        stops[0].AddedBy.Should().Be(StopAddedBy.User);

        // Update stop
        var updateRequest = new UpdateStopRequest
        {
            CustomName = "Updated Full Flow Stop",
            Notes = "Added some notes"
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/stops/{stopId}`, updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Mark visited
        var visitedResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/stops/{stopId}/visited", null);
        visitedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Delete stop
        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/stops/{stopId}`);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deleted
        var getAfterDeleteResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/stops");
        var getAfterDeleteBody = await getAfterDeleteResponse.Content.ReadAsStringAsync();
        var stopsAfterDelete = JsonSerializer.Deserialize<List<StopResponse>>(getAfterDeleteBody, _json);

        stopsAfterDelete!.Should().BeEmpty();
    }
}