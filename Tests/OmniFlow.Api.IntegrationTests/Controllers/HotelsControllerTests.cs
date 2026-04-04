using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Hotels;
using OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class HotelsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HotelsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    private async Task<string> GetAccessTokenAsync(string email = TestDatabaseSeeder.TestUserEmail, string password = TestDatabaseSeeder.TestUserPassword)
    {
        var loginRequest = new AuthenticationRequest
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/account/login", loginRequest);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, _json);

        return result!.AccessToken;
    }

    private async Task<(Guid tripId, List<Guid> hotelIds)> SeedTripWithHotelsAsync(Guid userId, TripStatus status = TripStatus.Published)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            Status = status,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure,
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14))
        };

        dbContext.Trips.Add(trip);

        var hotel1Id = Guid.NewGuid();
        var hotel2Id = Guid.NewGuid();

        // Helper functions for DateTime handling
        static DateTime GetLocalDateTime(int daysToAdd) =>
            DateTime.SpecifyKind(DateTime.Today.AddDays(daysToAdd), DateTimeKind.Unspecified);
        static DateTime GetUtcDateTime() => DateTime.UtcNow;

        var hotel1 = new Hotel
        {
            Id = hotel1Id,
            TripId = trip.Id,
            HotelName = "Luxury Resort",
            HotelAddress = "Beach Road 123",
            Stars = 5,
            RoomType = RoomType.Suite,
            BreakfastIncluded = true,
            CancellationPolicy = CancellationPolicy.Free,
            CheckIn = GetLocalDateTime(7),
            CheckOut = GetLocalDateTime(10),
            PricePerNight = 200,
            TotalPrice = 600,
            CurrencyCode = "USD",
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime()
        };

        var hotel2 = new Hotel
        {
            Id = hotel2Id,
            TripId = trip.Id,
            HotelName = "Budget Hotel",
            HotelAddress = "City Center 45",
            Stars = 3,
            RoomType = RoomType.Double,
            BreakfastIncluded = false,
            CancellationPolicy = CancellationPolicy.NonRefundable,
            CheckIn = GetLocalDateTime(7),
            CheckOut = GetLocalDateTime(10),
            PricePerNight = 80,
            TotalPrice = 240,
            CurrencyCode = "USD",
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime()
        };

        dbContext.Hotels.Add(hotel1);
        dbContext.Hotels.Add(hotel2);
        await dbContext.SaveChangesAsync();

        return (trip.Id, new List<Guid> { hotel1Id, hotel2Id });
    }

    private async Task<Guid> GetTestUserIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var user = dbContext.Users.FirstOrDefault(u => u.Email == TestDatabaseSeeder.TestUserEmail);
        return user!.Id;
    }

    // ── GET Hotels ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHotels_PublishedTrip_ReturnsHotels()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Published);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<HotelsByTripViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.Hotels.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHotels_DraftTrip_OwnerCanAccess()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHotels_NonExistentTrip_Returns404()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var nonExistentTripId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/trips/{nonExistentTripId}/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHotels_Unauthenticated_Returns401()
    {
        // Arrange
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Published);

        // Act - No auth header
        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST Select Hotel ────────────────────────────────────────────────────────

    [Fact]
    public async Task SelectHotel_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, hotelIds) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SelectHotelRequest
        {
            HotelId = hotelIds[0]
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/hotels/select", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SelectHotel_NonOwner_Returns403()
    {
        // Arrange - Create a trip owned by the test user
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, hotelIds) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        // Login as a different user (admin) to test non-owner access
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var request = new SelectHotelRequest
        {
            HotelId = hotelIds[0]
        };

        // Act - Admin user tries to select hotel for test user's trip
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/hotels/select", request);

        // Assert - Should be forbidden since admin is not the owner
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SelectHotel_NonExistentHotel_Returns404()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SelectHotelRequest
        {
            HotelId = Guid.NewGuid() // Non-existent hotel
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/hotels/select", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SelectHotel_NonExistentTrip_Returns404()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SelectHotelRequest
        {
            HotelId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{Guid.NewGuid()}/hotels/select", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SelectHotel_InvalidRequest_Returns400()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new SelectHotelRequest
        {
            HotelId = Guid.Empty // Invalid - empty GUID
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/hotels/select", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SelectHotel_Unauthenticated_Returns401()
    {
        // Arrange
        var userId = await GetTestUserIdAsync();
        var (tripId, hotelIds) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        var request = new SelectHotelRequest
        {
            HotelId = hotelIds[0]
        };

        // Act - No auth header
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/hotels/select", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}