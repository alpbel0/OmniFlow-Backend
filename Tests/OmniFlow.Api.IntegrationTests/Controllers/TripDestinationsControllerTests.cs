using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.TripDestinations;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class TripDestinationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TripDestinationsControllerTests(CustomWebApplicationFactory factory)
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

        // Get the first (and only) destination
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(getBody, _json);
        var destinationId = destinations!.First().Id;

        return (tripId, destinationId);
    }

    private async Task<(Guid TripId, List<TripDestinationResponse> Destinations)> CreateDraftTripWithThreeDestinationsAsync(HttpClient authClient)
    {
        var createRequest = new CreateTripWizardRequest
        {
            Title = "Draft Multi Destination Trip",
            Description = "Trip used for reorder tests",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelCompanion = TravelCompanion.Couple,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural },
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.Walking,
            ManualBudget = 1500,
            CoverPhotoUrl = "https://example.com/cover.jpg",
            Destinations =
            [
                new CreateTripDestinationRequest
                {
                    City = "Istanbul",
                    Country = "Turkey",
                    ArrivalDate = new DateOnly(2026, 8, 10),
                    DepartureDate = new DateOnly(2026, 8, 12),
                    OrderIndex = 1
                },
                new CreateTripDestinationRequest
                {
                    City = "Rome",
                    Country = "Italy",
                    ArrivalDate = new DateOnly(2026, 8, 12),
                    DepartureDate = new DateOnly(2026, 8, 15),
                    OrderIndex = 2
                },
                new CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = new DateOnly(2026, 8, 15),
                    DepartureDate = new DateOnly(2026, 8, 15),
                    OrderIndex = 3
                }
            ]
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips/wizard", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(createBody, _json);

        return (createResult!.TripId, createResult.Destinations.OrderBy(d => d.OrderIndex).ToList());
    }

    private async Task<(Guid TripId, Guid DestinationId)> CreatePublishedTripWithDestinationAsync(HttpClient authClient)
    {
        var (tripId, destinationId) = await CreateDraftTripWithDestinationAsync(authClient);

        // Add a timeline entry so the trip can be published
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var trip = db.Trips.First(t => t.Id == tripId);
        trip.Description = "Publishable trip for destination tests";
        trip.CoverPhotoUrl = "https://example.com/cover.jpg";
        trip.EstimatedCost = 1200;
        var firstEntry = TimelineEntry.CreateCustomEventEntry(tripId, destinationId, 1, 1000.0, "Event", new TimeOnly(10, 0), 60);
        var secondEntry = TimelineEntry.CreateCustomEventEntry(tripId, destinationId, 1, 1001.0, "Dinner", new TimeOnly(19, 0), 60);
        await db.TimelineEntries.AddRangeAsync(firstEntry, secondEntry);
        await db.SaveChangesAsync();

        // Publish
        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return (tripId, destinationId);
    }

    // ── GET Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDestinations_PublishedTrip_NoToken_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(authClient);

        // Call without token
        var response = await _client.GetAsync($"/api/v1/trips/{tripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations.Should().NotBeNull();
        destinations!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDestinations_PublishedTrip_Owner_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations.Should().NotBeNull();
        destinations!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDestinations_PublishedTrip_OtherUser_Returns200()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(ownerClient);

        // Other user (admin)
        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations.Should().NotBeNull();
        destinations!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDestinations_DraftTrip_Owner_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations.Should().NotBeNull();
        destinations!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDestinations_DraftTrip_OtherUser_Returns404()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(ownerClient);

        // Other user (admin)
        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDestinations_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var fakeTripId = Guid.NewGuid();
        var response = await authClient.GetAsync($"/api/v1/trips/{fakeTripId}/destinations");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDestination_WithoutToken_Returns401()
    {
        var request = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 1
        };

        var fakeTripId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/api/v1/trips/{fakeTripId}/destinations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDestination_OwnerDraft_Returns201()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await response.Content.ReadAsStringAsync();
        var createdDestination = JsonSerializer.Deserialize<TripDestinationResponse>(createBody, _json);
        createdDestination.Should().NotBeNull();
        createdDestination!.Id.Should().NotBeEmpty();
        createdDestination.City.Should().Be("Paris");
        createdDestination.Country.Should().Be("France");
        createdDestination.ArrivalDate.Should().Be(request.ArrivalDate);
        createdDestination.DepartureDate.Should().Be(request.DepartureDate);
        createdDestination.OrderIndex.Should().Be(2);

        // Verify it was added
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations!.Should().HaveCount(2);
        var paris = destinations.Single(d => d.City == "Paris");
        paris.Latitude.Should().Be(41.0082);
        paris.Longitude.Should().Be(28.9784);
    }

    [Fact]
    public async Task CreateDestination_Published_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreatePublishedTripWithDestinationAsync(authClient);

        var request = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDestination_OtherUser_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(ownerClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var request = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };

        var response = await otherClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateDestination_InvalidDates_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 10),
            DepartureDate = new DateOnly(2026, 9, 1), // Invalid: departure < arrival
            OrderIndex = 2
        };

        var response = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateDestination_OrderIndexShift_Returns201()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, _) = await CreateDraftTripWithDestinationAsync(authClient);

        // Add second destination at index 2
        var request2 = new CreateTripDestinationRequest
        {
            City = "Rome",
            Country = "Italy",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };

        var response2 = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Now insert at index 1 — existing should shift
        var request3 = new CreateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 6),
            DepartureDate = new DateOnly(2026, 9, 10),
            OrderIndex = 1
        };

        var response3 = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", request3);
        response3.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify order
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations!.Should().HaveCount(3);
        destinations!.Select(d => d.OrderIndex).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        destinations.First(d => d.OrderIndex == 1).City.Should().Be("Paris");
        destinations.First(d => d.OrderIndex == 2).City.Should().Be("Istanbul");
        destinations.First(d => d.OrderIndex == 3).City.Should().Be("Rome");
    }

    // ── PUT Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderDestinations_WithoutToken_Returns401()
    {
        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = [Guid.NewGuid()]
        };

        var response = await _client.PutAsJsonAsync($"/api/v1/Trips/{Guid.NewGuid()}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReorderDestinations_OwnerDraft_ShiftsDatesAndTimelineDays()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinations) = await CreateDraftTripWithThreeDestinationsAsync(authClient);
        var istanbul = destinations.First(d => d.City == "Istanbul");
        var rome = destinations.First(d => d.City == "Rome");
        var paris = destinations.First(d => d.City == "Paris");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            await db.TimelineEntries.AddRangeAsync(
                TimelineEntry.CreateCustomEventEntry(tripId, istanbul.Id, 1, 1000.0, "Istanbul Event", new TimeOnly(10, 0), 60),
                TimelineEntry.CreateCustomEventEntry(tripId, rome.Id, 3, 1000.0, "Rome Event", new TimeOnly(12, 0), 60));
            await db.SaveChangesAsync();
        }

        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = [rome.Id, istanbul.Id, paris.Id]
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/Trips/{tripId}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var updatedDestinations = verifyDb.TripDestinations
            .Where(d => d.TripId == tripId && d.DeletedAt == null)
            .OrderBy(d => d.OrderIndex)
            .ToList();

        updatedDestinations.Select(d => d.Id).Should().ContainInOrder(rome.Id, istanbul.Id, paris.Id);
        updatedDestinations[0].ArrivalDate.Should().Be(new DateOnly(2026, 8, 10));
        updatedDestinations[0].DepartureDate.Should().Be(new DateOnly(2026, 8, 13));
        updatedDestinations[0].NightCount.Should().Be(3);
        updatedDestinations[1].ArrivalDate.Should().Be(new DateOnly(2026, 8, 13));
        updatedDestinations[1].DepartureDate.Should().Be(new DateOnly(2026, 8, 15));
        updatedDestinations[1].NightCount.Should().Be(2);
        updatedDestinations[2].ArrivalDate.Should().Be(new DateOnly(2026, 8, 15));
        updatedDestinations[2].DepartureDate.Should().Be(new DateOnly(2026, 8, 15));
        updatedDestinations[2].NightCount.Should().Be(0);

        var updatedEntries = verifyDb.TimelineEntries
            .Where(e => e.TripId == tripId && e.DeletedAt == null)
            .ToList();
        updatedEntries.First(e => e.DestinationId == rome.Id).DayNumber.Should().Be(1);
        updatedEntries.First(e => e.DestinationId == istanbul.Id).DayNumber.Should().Be(4);

        var updatedTrip = verifyDb.Trips.First(t => t.Id == tripId);
        updatedTrip.StartDate.Should().Be(new DateOnly(2026, 8, 10));
        updatedTrip.EndDate.Should().Be(new DateOnly(2026, 8, 15));
    }

    [Fact]
    public async Task ReorderDestinations_DuplicateIds_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinations) = await CreateDraftTripWithThreeDestinationsAsync(authClient);

        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = [destinations[0].Id, destinations[0].Id, destinations[1].Id]
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/Trips/{tripId}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderDestinations_MissingDestination_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinations) = await CreateDraftTripWithThreeDestinationsAsync(authClient);

        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = [destinations[1].Id, destinations[0].Id]
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/Trips/{tripId}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderDestinations_PublishedTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var (tripId, destinationId) = await CreatePublishedTripWithDestinationAsync(authClient);

        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = [destinationId]
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/Trips/{tripId}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderDestinations_OtherUser_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var (tripId, destinations) = await CreateDraftTripWithThreeDestinationsAsync(ownerClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var request = new ReorderDestinationsRequest
        {
            OrderedDestinationIds = destinations.Select(d => d.Id).Reverse().ToList()
        };

        var response = await otherClient.PutAsJsonAsync($"/api/v1/Trips/{tripId}/destinations/reorder", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDestination_WithoutToken_Returns401()
    {
        var request = new UpdateTripDestinationRequest
        {
            City = "Paris",
            Country = "France",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 1
        };

        var fakeTripId = Guid.NewGuid();
        var fakeDestId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/api/v1/trips/{fakeTripId}/destinations/{fakeDestId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateDestination_OwnerDraft_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, destId) = await CreateDraftTripWithDestinationAsync(authClient);

        var request = new UpdateTripDestinationRequest
        {
            City = "Updated City",
            Country = "Updated Country",
            ArrivalDate = new DateOnly(2026, 8, 15),
            DepartureDate = new DateOnly(2026, 8, 20),
            OrderIndex = 1
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/destinations/{destId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        var updatedDestination = destinations!.First();
        updatedDestination.City.Should().Be("Updated City");
        updatedDestination.Latitude.Should().Be(41.0082);
        updatedDestination.Longitude.Should().Be(28.9784);
    }

    [Fact]
    public async Task UpdateDestination_Published_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, destId) = await CreatePublishedTripWithDestinationAsync(authClient);

        var request = new UpdateTripDestinationRequest
        {
            City = "Updated City",
            Country = "Updated Country",
            ArrivalDate = new DateOnly(2026, 8, 15),
            DepartureDate = new DateOnly(2026, 8, 20),
            OrderIndex = 1
        };

        var response = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/destinations/{destId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateDestination_OtherUser_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var (tripId, destId) = await CreateDraftTripWithDestinationAsync(ownerClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var request = new UpdateTripDestinationRequest
        {
            City = "Updated City",
            Country = "Updated Country",
            ArrivalDate = new DateOnly(2026, 8, 15),
            DepartureDate = new DateOnly(2026, 8, 20),
            OrderIndex = 1
        };

        var response = await otherClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/destinations/{destId}", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateDestination_OrderIndexShift_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, destId1) = await CreateDraftTripWithDestinationAsync(authClient);

        // Add second destination
        var createRequest = new CreateTripDestinationRequest
        {
            City = "Rome",
            Country = "Italy",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };
        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get second destination id
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        var destId2 = destinations.First(d => d.City == "Rome").Id;

        // Move Rome to position 1 (shift Istanbul to 2)
        var updateRequest = new UpdateTripDestinationRequest
        {
            City = "Rome",
            Country = "Italy",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 1
        };

        var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/trips/{tripId}/destinations/{destId2}", updateRequest);
        if (updateResponse.StatusCode != HttpStatusCode.NoContent)
        {
            var errorBody = await updateResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Expected 204 but got {updateResponse.StatusCode}. Body: {errorBody}");
        }
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify order shifted
        var getResponse2 = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body2 = await getResponse2.Content.ReadAsStringAsync();
        var updatedDestinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body2, _json);
        updatedDestinations!.Should().HaveCount(2);
        updatedDestinations.First(d => d.City == "Rome").OrderIndex.Should().Be(1);
        updatedDestinations.First(d => d.City == "Istanbul").OrderIndex.Should().Be(2);
    }

    // ── DELETE Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteDestination_WithoutToken_Returns401()
    {
        var fakeTripId = Guid.NewGuid();
        var fakeDestId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/v1/trips/{fakeTripId}/destinations/{fakeDestId}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteDestination_OwnerDraft_Returns204()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var (tripId, destId) = await CreateDraftTripWithDestinationAsync(authClient);

        // Add second destination
        var createRequest = new CreateTripDestinationRequest
        {
            City = "Rome",
            Country = "Italy",
            ArrivalDate = new DateOnly(2026, 9, 1),
            DepartureDate = new DateOnly(2026, 9, 5),
            OrderIndex = 2
        };
        var createResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/destinations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Delete first destination
        var deleteResponse = await authClient.DeleteAsync($"/api/v1/trips/{tripId}/destinations/{destId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify: only Rome remains and its OrderIndex shifted to 1
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var body = await getResponse.Content.ReadAsStringAsync();
        var destinations = JsonSerializer.Deserialize<List<TripDestinationResponse>>(body, _json);
        destinations!.Should().HaveCount(1);
        destinations!.First().City.Should().Be("Rome");
        destinations.First().OrderIndex.Should().Be(1);
    }

    [Fact]
    public async Task DeleteDestination_OtherUser_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);

        var (tripId, destId) = await CreateDraftTripWithDestinationAsync(ownerClient);

        var otherToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var otherClient = CreateAuthenticatedClient(otherToken);

        var response = await otherClient.DeleteAsync($"/api/v1/trips/{tripId}/destinations/{destId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
