using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.DTOs.TripDestinations;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Queries.GetMyTrips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
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
            Origin = "Antalya",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/trips", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithValidToken_ReturnsWizardResponse()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripRequest
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 6, 1),
            EndDate = new DateOnly(2026, 6, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateTripWizardResponse>(body, _json);

        result.Should().NotBeNull();
        result!.TripId.Should().NotBe(Guid.Empty);
        result.Status.Should().Be(TripStatus.Draft);
        result.Destinations.Should().HaveCount(1);
        result.Destinations[0].City.Should().Be("Antalya");
    }

    [Fact]
    public async Task Create_WithInvalidData_Returns422()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripRequest
        {
            Title = "", // Invalid: empty title
            Origin = "Antalya",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
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
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 6, 10),
            EndDate = new DateOnly(2026, 6, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
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
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 3,
            BudgetTier = BudgetTier.Premium,
            TravelStyles = new List<TravelStyle> { TravelStyle.Relax }
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
            Origin = "Izmir",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 5),
            PersonCount = 4,
            BudgetTier = BudgetTier.Premium,
            TravelStyles = new List<TravelStyle> { TravelStyle.Relax },
            Description = "A relaxing beach vacation"
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(createBody, _json);
        var tripId = createResult!.TripId;

        // Get by Id
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var trip = JsonSerializer.Deserialize<TripResponse>(getBody, _json);

        trip.Should().NotBeNull();
        trip!.Title.Should().Be("Full Flow Trip");
        trip.Origin.Should().Be("Izmir");
        trip.Status.Should().Be(TripStatus.Draft);

        // Update
        var updateRequest = new UpdateTripRequest
        {
            Title = "Updated Full Flow Trip",
            Origin = "Izmir",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 5),
            PersonCount = 5,
            BudgetTier = BudgetTier.Premium,
            TravelStyles = new List<TravelStyle> { TravelStyle.Relax },
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

    // ── Owner Authorization Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_OtherUserTrip_Returns403()
    {
        // Create trip with test user
        var testUserToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var testUserClient = CreateAuthenticatedClient(testUserToken);

        var createRequest = new CreateTripRequest
        {
            Title = "Test User Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var createResponse = await testUserClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Try to update with admin user
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var updateRequest = new UpdateTripRequest
        {
            Title = "Hacked Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 5),
            PersonCount = 2
        };

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/trips/{tripId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_OtherUserTrip_Returns403()
    {
        // Create trip with test user
        var testUserToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var testUserClient = CreateAuthenticatedClient(testUserToken);

        var createRequest = new CreateTripRequest
        {
            Title = "Test User Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 5),
            PersonCount = 2
        };

        var createResponse = await testUserClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Try to delete with admin user
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var deleteResponse = await adminClient.DeleteAsync($"/api/v1/trips/{tripId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Fork Tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fork_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/fork", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Fork_NonExistentTrip_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.PostAsync($"/api/v1/trips/{Guid.NewGuid()}/fork", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Fork_DraftTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create a draft trip (no stops)
        var createRequest = new CreateTripRequest
        {
            Title = "Draft Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 5),
            PersonCount = 2
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Try to fork draft trip
        var forkResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/fork", null);
        forkResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Fork_SelfFork_Returns409()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create and publish trip
        var tripId = await CreateAndPublishTripAsync(authClient);

        // Try to fork own trip
        var forkResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/fork", null);
        forkResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Fork_Success_Returns201()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by admin
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);
        var tripId = await CreateAndPublishTripAsync(adminClient);

        // Fork the trip as test user
        var forkResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/fork", null);
        forkResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var forkedTripId = JsonSerializer.Deserialize<Guid>(await forkResponse.Content.ReadAsStringAsync());
        forkedTripId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Fork_CopiesDestinationsAndTimelineEntriesAndResetsCounters()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create published trip owned by admin with multiple timeline entries
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var adminClient = CreateAuthenticatedClient(adminToken);
        var tripId = await CreateAndPublishTripAsync(adminClient, entryCount: 3);

        // Get original trip fork count
        var getOriginalResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        var originalTrip = JsonSerializer.Deserialize<TripResponse>(await getOriginalResponse.Content.ReadAsStringAsync(), _json);
        var originalForkCount = originalTrip!.ForkCount;

        // Fork the trip
        var forkResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/fork", null);
        var forkedTripId = JsonSerializer.Deserialize<Guid>(await forkResponse.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var originalDestinations = db.TripDestinations
            .Where(d => d.TripId == tripId && d.DeletedAt == null)
            .OrderBy(d => d.OrderIndex)
            .ToList();
        var forkedDestinations = db.TripDestinations
            .Where(d => d.TripId == forkedTripId && d.DeletedAt == null)
            .OrderBy(d => d.OrderIndex)
            .ToList();

        var originalTimelineEntries = db.TimelineEntries
            .Where(e => e.TripId == tripId && e.DeletedAt == null)
            .OrderBy(e => e.DayNumber)
            .ThenBy(e => e.OrderIndex)
            .ToList();
        var forkedTimelineEntries = db.TimelineEntries
            .Where(e => e.TripId == forkedTripId && e.DeletedAt == null)
            .OrderBy(e => e.DayNumber)
            .ThenBy(e => e.OrderIndex)
            .ToList();

        // Verify forked trip
        var getForkedResponse = await authClient.GetAsync($"/api/v1/trips/{forkedTripId}");
        var forkedTrip = JsonSerializer.Deserialize<TripResponse>(await getForkedResponse.Content.ReadAsStringAsync(), _json);

        forkedTrip!.Status.Should().Be(TripStatus.Draft);
        forkedTrip.ForkCount.Should().Be(0);
        forkedTrip.UpvoteCount.Should().Be(0);
        forkedTrip.ViewCount.Should().Be(0);
        forkedTrip.ForkedFromId.Should().Be(tripId);
        forkedDestinations.Should().HaveCount(originalDestinations.Count);
        forkedDestinations.Select(d => (d.City, d.Country, d.ArrivalDate, d.DepartureDate, d.OrderIndex))
            .Should().BeEquivalentTo(originalDestinations.Select(d => (d.City, d.Country, d.ArrivalDate, d.DepartureDate, d.OrderIndex)));
        forkedTimelineEntries.Should().HaveCount(originalTimelineEntries.Count);
        forkedTimelineEntries.Select(e => (e.DayNumber, e.OrderIndex, e.EntryType, e.CustomName, e.IsVisited))
            .Should().BeEquivalentTo(originalTimelineEntries.Select(e => (e.DayNumber, e.OrderIndex, e.EntryType, e.CustomName, IsVisited: false)));
        forkedTimelineEntries.Should().OnlyContain(e => !e.IsVisited);
        forkedTimelineEntries.Select(e => e.DestinationId).Distinct().Should().OnlyContain(id => forkedDestinations.Select(d => d.Id).Contains(id));

        // Verify original trip fork count incremented
        var getOriginalAfterResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        var originalTripAfter = JsonSerializer.Deserialize<TripResponse>(await getOriginalAfterResponse.Content.ReadAsStringAsync(), _json);
        originalTripAfter!.ForkCount.Should().Be(originalForkCount + 1);
    }

    // ── Task 4.1: Wizard + Budget + Recommendation + Timeline Summary Tests ─────────

    [Fact]
    public async Task CreateWizard_FullFlow_ReturnsBudgetMessagesAndDestinations()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var request = new CreateTripWizardRequest
        {
            Title = "Wizard Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Premium,
            TravelCompanion = TravelCompanion.Couple,
            TravelStyles = new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Cultural },
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.Walking,
            ManualBudget = 500,
            Destinations =
            [
                new OmniFlow.Application.DTOs.TripDestinations.CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = new DateOnly(2026, 6, 10),
                    DepartureDate = new DateOnly(2026, 6, 15),
                    OrderIndex = 1
                }
            ]
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips/wizard", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateTripWizardResponse>(body, _json);

        result.Should().NotBeNull();
        result!.TripId.Should().NotBe(Guid.Empty);
        result.Destinations.Should().HaveCount(1);
        result.Destinations[0].City.Should().Be("Paris");
        result.BudgetMessages.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBudgetSummary_ForOwnTrip_ReturnsCorrectSummary()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create trip via old endpoint
        var createRequest = new CreateTripRequest
        {
            Title = "Budget Test Trip",
            Origin = "Rome",
            OriginCountry = "Italy",
            StartDate = new DateOnly(2026, 7, 1),
            EndDate = new DateOnly(2026, 7, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Get budget summary
        var budgetResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/budget-summary");
        budgetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var budgetBody = await budgetResponse.Content.ReadAsStringAsync();
        var budgetResult = JsonSerializer.Deserialize<BudgetSummaryResponse>(budgetBody, _json);

        budgetResult.Should().NotBeNull();
        budgetResult!.TotalCost.Should().BeGreaterThanOrEqualTo(0);
        budgetResult.BudgetTier.Should().Be(BudgetTier.Standard);
    }

    [Fact]
    public async Task GetBudgetSummary_ForProviderHotelEntry_UsesEntryNightCount()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var createRequest = new CreateTripWizardRequest
        {
            Title = "Provider Hotel Night Count Trip",
            Origin = "Rome",
            OriginCountry = "Italy",
            PersonCount = 1,
            BudgetTier = BudgetTier.Standard,
            Destinations =
            [
                new CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = new DateOnly(2026, 9, 10),
                    DepartureDate = new DateOnly(2026, 9, 15),
                    OrderIndex = 1
                }
            ]
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips/wizard", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(
            await createResponse.Content.ReadAsStringAsync(),
            _json);

        createResult.Should().NotBeNull();

        var tripId = createResult!.TripId;
        var destinationId = createResult.Destinations.Single().Id;

        var timelineCreateRequest = new CreateTimelineEntryRequest
        {
            TripId = tripId,
            DestinationId = destinationId,
            DayNumber = 1,
            EntryType = TimelineEntryType.CustomAccommodation,
            ProviderHotelId = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
            CurrencyCode = "USD",
            Notes = "Single night provider hotel"
        };

        var timelineResponse = await authClient.PostAsJsonAsync(
            $"/api/v1/trips/{tripId}/timeline/entry",
            timelineCreateRequest);

        timelineResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var budgetResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/budget-summary");
        budgetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var budgetResult = JsonSerializer.Deserialize<BudgetSummaryResponse>(
            await budgetResponse.Content.ReadAsStringAsync(),
            _json);

        budgetResult.Should().NotBeNull();
        budgetResult!.TotalHotelCost.Should().Be(80);
        budgetResult.TotalCost.Should().Be(80);
    }

    [Fact]
    public async Task GetById_ReturnsTimelineSummary_WhenEntriesExist()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create trip
        var createRequest = new CreateTripRequest
        {
            Title = "Timeline Summary Trip",
            Origin = "Barcelona",
            OriginCountry = "Spain",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Economy,
            TravelStyles = new List<TravelStyle> { TravelStyle.Budget }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Add timeline entries directly via DB context
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dest = db.TripDestinations.First(d => d.TripId == tripId);

        var entry1 = TimelineEntry.CreateCustomEventEntry(tripId, dest.Id, 1, 1000.0, "Event 1", new TimeOnly(10, 0), 60);
        var entry2 = TimelineEntry.CreateCustomEventEntry(tripId, dest.Id, 1, 1001.0, "Event 2", new TimeOnly(12, 0), 60);
        var entry3 = TimelineEntry.CreateCustomEventEntry(tripId, dest.Id, 2, 1000.0, "Event 3", new TimeOnly(10, 0), 60);

        db.TimelineEntries.Add(entry1);
        db.TimelineEntries.Add(entry2);
        db.TimelineEntries.Add(entry3);
        await db.SaveChangesAsync();

        // Get trip by id
        var getResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadAsStringAsync();
        var trip = JsonSerializer.Deserialize<TripResponse>(getBody, _json);

        trip.Should().NotBeNull();
        trip!.TimelineSummary.Should().NotBeNull();
        trip.TimelineSummary!.TotalEntryCount.Should().Be(3);
        trip.TimelineSummary.DailyCounts.Should().HaveCount(2);
        trip.TimelineSummary.DailyCounts.Should().Contain(d => d.DayNumber == 1 && d.EntryCount == 2);
        trip.TimelineSummary.DailyCounts.Should().Contain(d => d.DayNumber == 2 && d.EntryCount == 1);
    }

    [Fact]
    public async Task GetRecommendPlaces_ForPublishedTrip_ReturnsOk()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        // Create and publish trip
        var tripId = await CreateAndPublishTripAsync(authClient);

        // Seed a place for the destination city
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dest = db.TripDestinations.First(d => d.TripId == tripId);

        var place = new Place
        {
            City = dest.City,
            Country = dest.Country,
            Name = "Test Place",
            Category = PlaceCategory.Museum,
            Latitude = 41.0,
            Longitude = 28.0,
            Rating = 4.5m,
            IsFree = false,
            EstimatedPrice = 20,
            PhotoUrl = "https://example.com/photo.jpg",
            BudgetTiers = new List<BudgetTier> { BudgetTier.Standard },
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
        };
        db.Places.Add(place);
        await db.SaveChangesAsync();

        // Get recommendations
        var recResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/recommend-places?destinationId={dest.Id}");
        recResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var recBody = await recResponse.Content.ReadAsStringAsync();
        var recResult = JsonSerializer.Deserialize<RecommendedPlacesResult>(recBody, _json);

        recResult.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendPlaces_WithAccommodationHub_PrioritizesNearbyPlaceForWalking()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var createRequest = new CreateTripWizardRequest
        {
            Title = "Walking Hub Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelCompanion = TravelCompanion.Couple,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural },
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.Walking,
            Destinations =
            [
                new OmniFlow.Application.DTOs.TripDestinations.CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = new DateOnly(2026, 6, 10),
                    DepartureDate = new DateOnly(2026, 6, 15),
                    OrderIndex = 1
                }
            ]
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips/wizard", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;
        var destinationId = createResult.Destinations.Single().Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var accommodationEntry = TimelineEntry.CreateCustomAccommodationEntry(
                tripId,
                destinationId,
                1,
                1000,
                new DateTime(2026, 6, 10, 14, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
                "Paris Hub Hotel",
                "1 Rue de Hub",
                48.8566,
                2.3522);

            db.TimelineEntries.Add(accommodationEntry);

            db.Places.Add(new Place
            {
                Name = "Near Museum",
                City = "Paris",
                Country = "France",
                Category = PlaceCategory.Museum,
                Latitude = 48.8570,
                Longitude = 2.3530,
                Rating = 4.6m,
                IsFree = false,
                EstimatedPrice = 25,
                PhotoUrl = "https://example.com/near.jpg",
                BudgetTiers = new List<BudgetTier> { BudgetTier.Standard },
                TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
            });

            db.Places.Add(new Place
            {
                Name = "Far Museum",
                City = "Paris",
                Country = "France",
                Category = PlaceCategory.Museum,
                Latitude = 48.9350,
                Longitude = 2.5000,
                Rating = 4.6m,
                IsFree = false,
                EstimatedPrice = 25,
                PhotoUrl = "https://example.com/far.jpg",
                BudgetTiers = new List<BudgetTier> { BudgetTier.Standard },
                TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
            });

            await db.SaveChangesAsync();
        }

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var recResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/recommend-places?destinationId={destinationId}");
        recResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var recBody = await recResponse.Content.ReadAsStringAsync();
        var recResult = JsonSerializer.Deserialize<RecommendedPlacesResult>(recBody, _json);

        recResult.Should().NotBeNull();
        var orderedNames = recResult!.Recommended
            .Concat(recResult.Neutral)
            .Concat(recResult.Other)
            .Select(p => p.Name)
            .ToList();

        orderedNames.Should().Contain("Near Museum");
        orderedNames.Should().Contain("Far Museum");
        orderedNames.IndexOf("Near Museum").Should().BeLessThan(orderedNames.IndexOf("Far Museum"));
    }

    // ── Helper Methods ─────────────────────────────────────────────────────────────

    private async Task<Guid> CreateAndPublishTripAsync(HttpClient authClient, int entryCount = 1)
    {
        // Create trip
        var createRequest = new CreateTripRequest
        {
            Title = "Publishable Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2026, 11, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(await createResponse.Content.ReadAsStringAsync(), _json);
        var tripId = createResult!.TripId;

        // Add timeline entries via DB context
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dest = db.TripDestinations.First(d => d.TripId == tripId);

        for (int i = 0; i < entryCount; i++)
        {
            var entry = TimelineEntry.CreateCustomEventEntry(tripId, dest.Id, 1, 1000 + i, $"Event {i + 1}", new TimeOnly(10, 0), 60);
            await db.TimelineEntries.AddAsync(entry);
        }
        await db.SaveChangesAsync();

        // Publish trip
        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return tripId;
    }
}
