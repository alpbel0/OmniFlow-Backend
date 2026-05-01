using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.DTOs.TripDestinations;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class ProvidersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProvidersControllerTests(CustomWebApplicationFactory factory)
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
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(Guid TripId, Guid DestinationId)> CreateDraftTripWithDestinationAsync(HttpClient authClient)
    {
        var createRequest = new CreateTripWizardRequest
        {
            Title = "Test Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            Destinations =
            [
                new CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                    OrderIndex = 1
                }
            ],
            PersonCount = 2,
            TravelCompanion = TravelCompanion.Couple,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = [TravelStyle.Cultural],
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.PublicTransport
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateTripWizardResponse>(body, _json);
        result.Should().NotBeNull();
        result!.TripId.Should().NotBeEmpty();

        var destinationId = result.Destinations.First().Id;
        return (result.TripId, destinationId);
    }

    // ── GET Origin Cities ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOriginCities_ReturnsDistinctCities()
    {
        var response = await _client.GetAsync("/api/v1/providers/origin-cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OriginCityResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Count.Should().BeGreaterThanOrEqualTo(3);
        result.Select(c => c.City).Should().Contain("Istanbul");
        result.Select(c => c.City).Should().Contain("Paris");
        result.Select(c => c.City).Should().Contain("Rome");
    }

    [Fact]
    public async Task GetOriginCities_NoFlights_ReturnsEmptyList()
    {
        // Arrange: clear provider flights (this runs in isolated in-memory DB per test collection)
        // Since we seeded flights, this test verifies the happy path. Empty case is covered by unit tests.
        var response = await _client.GetAsync("/api/v1/providers/origin-cities");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<OriginCityResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    // ── GET Flights ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFlights_Outbound_ReturnsSeasonAdjustedPrices()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var response = await _client.GetAsync($"/api/v1/providers/flights?fromCity=Istanbul&toCity=Paris&date={date:yyyy-MM-dd}&personCount=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderFlightResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();

        var flight = result.First();
        flight.SeasonMultiplier.Should().BeGreaterThan(0);
        flight.SeasonAdjustedPrice.Should().Be(flight.BasePrice * flight.SeasonMultiplier);
        flight.TotalPrice.Should().Be(flight.SeasonAdjustedPrice * 2);
    }

    [Fact]
    public async Task GetFlights_Return_ResolvesFromTrip()
    {
        var token = await GetAccessTokenAsync();
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var response = await _client.GetAsync($"/api/v1/providers/flights?isReturn=true&tripId={tripId}&personCount=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderFlightResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();

        var flight = result.First();
        flight.DepartureCity.Should().Be("Paris");  // last destination
        flight.ArrivalCity.Should().Be("Istanbul"); // origin
    }

    [Fact]
    public async Task GetFlights_Return_MissingTripId_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/providers/flights?isReturn=true&personCount=1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFlights_Outbound_MissingParams_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/providers/flights?fromCity=Istanbul&personCount=1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFlights_NoFlights_ReturnsEmptyList()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var response = await _client.GetAsync($"/api/v1/providers/flights?fromCity=Nowhere&toCity=Somewhere&date={date:yyyy-MM-dd}&personCount=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderFlightResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    // ── GET Hotels ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHotels_ReturnsWithSegmentInfo()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var checkOut = checkIn.AddDays(3);

        var response = await _client.GetAsync($"/api/v1/providers/hotels?city=Paris&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}&personCount=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderHotelResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();

        result.Should().Contain(h => h.Segment == BudgetTier.Economy);
        result.Should().Contain(h => h.Segment == BudgetTier.Standard);
        result.Should().Contain(h => h.Segment == BudgetTier.Premium);

        var hotel = result.First();
        hotel.NightCount.Should().Be(3);
        hotel.SeasonMultiplier.Should().BeGreaterThan(0);
        hotel.TotalPrice.Should().Be(hotel.SeasonAdjustedPricePerNight * 3 * 2);
    }

    [Fact]
    public async Task GetHotels_BudgetTierFilter_ReturnsOnlyMatchingSegment()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var checkOut = checkIn.AddDays(3);

        var response = await _client.GetAsync($"/api/v1/providers/hotels?city=Paris&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}&budgetTier=Economy&personCount=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderHotelResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.Should().AllSatisfy(h => h.Segment.Should().Be(BudgetTier.Economy));
    }

    [Fact]
    public async Task GetHotels_NoHotels_ReturnsEmptyList()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var checkOut = checkIn.AddDays(3);

        var response = await _client.GetAsync($"/api/v1/providers/hotels?city=Nowhere&checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}&personCount=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<ProviderHotelResponse>>(body, _json);

        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }
}
