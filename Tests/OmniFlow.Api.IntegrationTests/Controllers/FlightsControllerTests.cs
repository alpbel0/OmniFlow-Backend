using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class FlightsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FlightsControllerTests(CustomWebApplicationFactory factory)
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

    private async Task<(Guid tripId, List<Guid> flightIds)> SeedTripWithFlightsAsync(Guid userId, TripStatus status = TripStatus.Published)
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

        var outboundFlightId = Guid.NewGuid();
        var returnFlightId = Guid.NewGuid();

        static DateTime GetLocalDateTime(int daysToAdd) =>
            DateTime.SpecifyKind(DateTime.Today.AddDays(daysToAdd), DateTimeKind.Unspecified);
        static DateTime GetUtcDateTime() => DateTime.UtcNow;

        dbContext.Flights.Add(new Flight
        {
            Id = outboundFlightId,
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetLocalDateTime(7),
            ArrivalAt = GetLocalDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime()
        });

        dbContext.Flights.Add(new Flight
        {
            Id = returnFlightId,
            TripId = trip.Id,
            FlightDirection = FlightDirection.Return,
            FromCity = "Antalya",
            FromAirport = "AYT",
            ToCity = "Istanbul",
            ToAirport = "IST",
            DepartureAt = GetLocalDateTime(14),
            ArrivalAt = GetLocalDateTime(14).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime()
        });

        await dbContext.SaveChangesAsync();

        return (trip.Id, new List<Guid> { outboundFlightId, returnFlightId });
    }

    private async Task<Guid> GetTestUserIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var user = dbContext.Users.FirstOrDefault(u => u.Email == TestDatabaseSeeder.TestUserEmail);
        return user!.Id;
    }

    [Fact]
    public async Task GetFlights_PublishedTrip_ReturnsFlights()
    {
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithFlightsAsync(userId, TripStatus.Published);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/flights");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FlightsByTripViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.OutboundFlights.Should().HaveCount(1);
        result.ReturnFlights.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFlights_DraftTrip_OwnerCanAccess()
    {
        var token = await GetAccessTokenAsync();
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithFlightsAsync(userId, TripStatus.Draft);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/flights");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFlights_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync();
        var nonExistentTripId = Guid.NewGuid();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/trips/{nonExistentTripId}/flights");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFlights_Unauthenticated_Returns401()
    {
        var userId = await GetTestUserIdAsync();
        var (tripId, _) = await SeedTripWithFlightsAsync(userId, TripStatus.Published);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/flights");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
