using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class PlacesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PlacesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    // ── Helper Methods ─────────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await loginResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, _json);
        return result!.AccessToken!;
    }

    private HttpClient CreateAuthenticatedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // ── GET All Places ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/places");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithValidToken_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/v1/places");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResponse<PlaceResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPageSize()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync("/api/v1/places?pageNumber=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResponse<PlaceResponse>>(body, _json);

        result!.PageSize.Should().Be(5);
    }

    // ── GET Place By Id ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/places/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/places/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithExistingId_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // First create a place (as admin)
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createRequest = new CreatePlaceRequest
        {
            Name = "Test Museum",
            Category = PlaceCategory.Museum,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8841,
            Longitude = 30.7056,
            EstimatedPrice = 50,
            IsFree = false
        };

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/places", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var placeId = JsonSerializer.Deserialize<Guid>(createBody);

        // Now get by id
        var response = await authClient.GetAsync($"/api/v1/places/{placeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PlaceResponse>(body, _json);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Museum");
        result.City.Should().Be("Antalya");
    }

    // ── GET Places By City ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCity_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/places/city/Antalya");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByCity_WithValidCity_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // First create a place (as admin)
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var createRequest = new CreatePlaceRequest
        {
            Name = "City Test Restaurant",
            Category = PlaceCategory.Restaurant,
            City = "Istanbul",
            Country = "Turkey",
            Latitude = 41.0082,
            Longitude = 28.9784,
            EstimatedPrice = 100,
            IsFree = false
        };

        await adminClient.PostAsJsonAsync("/api/v1/places", createRequest);

        // Now get by city - use same city as created place
        var response = await authClient.GetAsync("/api/v1/places/city/Istanbul");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResponse<PlaceResponse>>(body, _json);

        result.Should().NotBeNull();
        // May or may not have results depending on case-insensitive matching
        result!.Data.Should().NotBeNull();
    }

    // ── POST Create Place ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var request = new CreatePlaceRequest
        {
            Name = "Unauthorized Place",
            Category = PlaceCategory.Museum,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.0,
            Longitude = 30.0,
            EstimatedPrice = 0,
            IsFree = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/places", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsNonAdmin_Returns403()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreatePlaceRequest
        {
            Name = "Forbidden Place",
            Category = PlaceCategory.Cafe,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.0,
            Longitude = 30.0,
            EstimatedPrice = 25,
            IsFree = false
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/places", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_AsAdmin_Returns201()
    {
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var request = new CreatePlaceRequest
        {
            Name = "Admin Created Place",
            Category = PlaceCategory.Historical,
            City = "Ankara",
            Country = "Turkey",
            Latitude = 39.9334,
            Longitude = 32.8597,
            EstimatedPrice = 75,
            IsFree = false,
            Rating = 4.5m,
            DurationMinutes = 90
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/places", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var placeId = JsonSerializer.Deserialize<Guid>(body);

        placeId.Should().NotBe(Guid.Empty);

        // Verify location header (controller name in route)
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain(placeId.ToString());
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns422()
    {
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var request = new CreatePlaceRequest
        {
            Name = "", // Invalid: empty name
            Category = PlaceCategory.Museum,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.0,
            Longitude = 30.0,
            EstimatedPrice = 0,
            IsFree = true
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/places", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_WithInvalidCoordinates_Returns422()
    {
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var request = new CreatePlaceRequest
        {
            Name = "Invalid Coords Place",
            Category = PlaceCategory.Nature,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 999, // Invalid: out of range
            Longitude = 30.0,
            EstimatedPrice = 0,
            IsFree = true
        };

        var response = await adminClient.PostAsJsonAsync("/api/v1/places", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}