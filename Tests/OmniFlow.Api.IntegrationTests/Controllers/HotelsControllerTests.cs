using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
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
            Origin = "Antalya",
            OriginCountry = "Turkey",
            Status = status,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure },
        };

        dbContext.Trips.Add(trip);

        var hotel1Id = Guid.NewGuid();
        var hotel2Id = Guid.NewGuid();

        static DateTime GetLocalDateTime(int daysToAdd) =>
            DateTime.SpecifyKind(DateTime.Today.AddDays(daysToAdd), DateTimeKind.Unspecified);
        static DateTime GetUtcDateTime() => DateTime.UtcNow;

        dbContext.Hotels.Add(new Hotel
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
        });

        dbContext.Hotels.Add(new Hotel
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
        });

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

    [Fact]
    public async Task GetHotels_PublishedTrip_ReturnsHotels()
    {
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Published);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<HotelsByTripViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.Hotels.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHotels_DraftTrip_OwnerCanAccess()
    {
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHotels_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync();
        var nonExistentTripId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{nonExistentTripId}/hotels");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHotels_Unauthenticated_Returns401()
    {
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithHotelsAsync(userId, TripStatus.Published);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/hotels");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
