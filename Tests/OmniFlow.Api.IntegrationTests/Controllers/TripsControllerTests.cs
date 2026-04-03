using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Queries.GetMyTrips;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class TripsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TripsControllerTests(CustomWebApplicationFactory factory)
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

    // ── GET My Trips ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyTrips_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/trips");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyTrips_WithValidToken_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/v1/trips");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetMyTripsViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    // ── GET Trip By Id ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST Create Trip ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var request = new CreateTripRequest
        {
            Title = "Unauthorized Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var response = await _client.PostAsJsonAsync("/api/v1/trips", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithValidToken_Returns201()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripRequest
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

        var response = await authClient.PostAsJsonAsync("/api/v1/trips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var tripId = JsonSerializer.Deserialize<Guid>(body);

        tripId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripRequest
        {
            Title = "", // Invalid: empty title
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_WithEndDateBeforeStartDate_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripRequest
        {
            Title = "Invalid Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 7),
            EndDate = new DateOnly(2025, 6, 1), // Invalid: end before start
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── PUT Update Trip ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        var request = new UpdateTripRequest
        {
            Title = "Updated Trip",
            City = "Istanbul",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 7, 1),
            EndDate = new DateOnly(2025, 7, 7),
            PersonCount = 3,
            BudgetTier = BudgetTier.Premium,
            TravelStyle = TravelStyle.Luxury
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DELETE Trip ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/trips/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST Publish Trip ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Publish_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST Archive Trip ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Archive_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/archive", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Full Flow Test ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Create_GetById_Update_Delete()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create
        var createRequest = new CreateTripRequest
        {
            Title = "Full Flow Trip",
            City = "Izmir",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 8, 1),
            EndDate = new DateOnly(2025, 8, 5),
            PersonCount = 4,
            BudgetTier = BudgetTier.Premium,
            TravelStyle = TravelStyle.Relax,
            Description = "A relaxing beach vacation"
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var tripId = JsonSerializer.Deserialize<Guid>(createBody);

        // Get by Id
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var trip = JsonSerializer.Deserialize<TripResponse>(getBody, _json);

        trip.Should().NotBeNull();
        trip!.Title.Should().Be("Full Flow Trip");
        trip.City.Should().Be("Izmir");
        trip.Status.Should().Be(TripStatus.Draft);

        // Update
        var updateRequest = new UpdateTripRequest
        {
            Title = "Updated Full Flow Trip",
            City = "Izmir",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 8, 1),
            EndDate = new DateOnly(2025, 8, 10), // Extended end date
            PersonCount = 5,
            BudgetTier = BudgetTier.Premium,
            TravelStyle = TravelStyle.Relax,
            Description = "An extended relaxing beach vacation"
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getAfterUpdateResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        var getAfterUpdateBody = await getAfterUpdateResponse.Content.ReadAsStringAsync();
        var updatedTrip = JsonSerializer.Deserialize<TripResponse>(getAfterUpdateBody, _json);

        updatedTrip!.Title.Should().Be("Updated Full Flow Trip");
        updatedTrip.PersonCount.Should().Be(5);

        // Delete
        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify delete (should return 404)
        var getAfterDeleteResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}