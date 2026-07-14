using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Currency;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.DTOs.VisitLogs;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Contexts;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public sealed class LiveTripCurrencyControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly CustomWebApplicationFactory _factory;

    public LiveTripCurrencyControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        DeleteStaleSeededTripsAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task CurrencyRates_UsesPublicBaseQueryContract()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/currency/rates?base=TRY&date=2026-07-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CurrencyRatesResponse>(Json);
        body!.BaseCurrencyCode.Should().Be("TRY");
        body.Rates.Select(x => x.QuoteCurrencyCode).Should().BeEquivalentTo("TRY", "USD", "EUR");
        body.Rates.Should().OnlyContain(x => x.EffectiveDate == new DateOnly(2026, 7, 10));
    }

    [Fact]
    public async Task Swagger_ContainsLiveTripAndCurrencyContracts()
    {
        using var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        paths.TryGetProperty("/api/v1/currency/rates", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/users/me/currency-preference", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/trips/{tripId}/visit-logs", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/trips/{tripId}/summary", out _).Should().BeTrue();
        paths.TryGetProperty("/api/v1/trips/{tripId}/nearby-places/search", out _).Should().BeTrue();
    }

    [Fact]
    public async Task NearbyPlaces_ReturnsOnlyEligiblePlacesWithoutCachingCoordinates()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync();
        var placeIds = Array.Empty<Guid>();
        try
        {
            placeIds = await SeedNearbyPlacesAsync(tripId, destinationId);
            var response = await client.PostAsJsonAsync(
                $"/api/v1/trips/{tripId}/nearby-places/search",
                new SearchNearbyPlacesRequest
                {
                    TripDestinationId = destinationId,
                    Latitude = 41.0082,
                    Longitude = 28.9784,
                    RadiusKm = 1,
                    CategoryGroup = NearbyPlaceCategoryGroup.FoodDrink
                });
            var places = await response.Content.ReadFromJsonAsync<List<NearbyPlaceResponse>>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.CacheControl!.NoStore.Should().BeTrue();
            response.Headers.CacheControl.Private.Should().BeTrue();
            places.Should().HaveCountLessThanOrEqualTo(20);
            places.Should().Contain(place => place.Id == placeIds[0]);
            places.Should().NotContain(place => placeIds.Skip(1).Contains(place.Id));
            var eligible = places!.Single(place => place.Id == placeIds[0]);
            eligible.IsPreviouslyVisited.Should().BeFalse();
            eligible.PreviousVisitCount.Should().Be(0);
            eligible.DistanceMeters.Should().BeGreaterThanOrEqualTo(0);
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
            await DeletePlacesAsync(placeIds);
        }
    }

    [Fact]
    public async Task NearbyPlaces_DraftTrip_ReturnsStableConflictCode()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync();
        try
        {
            var response = await client.PostAsJsonAsync(
                $"/api/v1/trips/{tripId}/nearby-places/search",
                new SearchNearbyPlacesRequest
                {
                    TripDestinationId = destinationId,
                    Latitude = 41.0082,
                    Longitude = 28.9784,
                    RadiusKm = 1,
                    CategoryGroup = NearbyPlaceCategoryGroup.All
                });
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            error!.Code.Should().Be("TRIP_NOT_PUBLISHED");
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Theory]
    [InlineData(1, "TRIP_NOT_STARTED")]
    [InlineData(-1, "TRIP_COMPLETED")]
    public async Task NearbyPlaces_NonActiveTrip_ReturnsTemporalConflictCode(
        int localDayOffset,
        string expectedCode)
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync(localDayOffset);
        try
        {
            await SetTripStatusAsync(tripId, TripStatus.Published);
            var response = await SearchNearbyAsync(client, tripId, destinationId, 41.0082, 28.9784);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            error!.Code.Should().Be(expectedCode);
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Fact]
    public async Task NearbyPlaces_MissingTimezone_ReturnsStableConflictCode()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync();
        try
        {
            await SetTripStatusAsync(tripId, TripStatus.Published);
            await ClearDestinationTimezoneAsync(destinationId);
            var response = await SearchNearbyAsync(client, tripId, destinationId, 41.0082, 28.9784);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            error!.Code.Should().Be("TIMEZONE_UNAVAILABLE");
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Fact]
    public async Task NearbyPlaces_SearchCenterBeyondTwentyFiveKm_ReturnsValidationCode()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync();
        try
        {
            await SetTripStatusAsync(tripId, TripStatus.Published);
            var response = await SearchNearbyAsync(client, tripId, destinationId, 41.30, 28.9784);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error!.Code.Should().Be("SEARCH_CENTER_OUTSIDE_DESTINATION_AREA");
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Fact]
    public async Task NearbyPlaces_NonOwnerIsForbidden()
    {
        using var client = await CreateAuthenticatedClientAsync(
            TestDatabaseSeeder.AdminEmail,
            TestDatabaseSeeder.AdminPassword);
        var (tripId, destinationId, _, _) = await SeedActiveTripAsync();
        try
        {
            await SetTripStatusAsync(tripId, TripStatus.Published);
            var response = await SearchNearbyAsync(client, tripId, destinationId, 41.0082, 28.9784);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Fact]
    public async Task NearbyPlaces_AnonymousRequestIsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await SearchNearbyAsync(
            client, Guid.NewGuid(), Guid.NewGuid(), 41.0082, 28.9784);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CurrencyPreference_UpdatesProfile()
    {
        using var client = await CreateAuthenticatedClientAsync();

        var update = await client.PutAsJsonAsync(
            "/api/v1/users/me/currency-preference",
            new UpdateCurrencyPreferenceRequest { CurrencyCode = "EUR" });
        var profile = await client.GetFromJsonAsync<UserProfileResponse>("/api/v1/users/me", Json);

        update.StatusCode.Should().Be(HttpStatusCode.NoContent);
        profile!.PreferredCurrencyCode.Should().Be("EUR");
    }

    [Fact]
    public async Task VisitLog_CreateListAndSummary_StayConsistent()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, destinationId, entryId, visitedAt) = await SeedActiveTripAsync();
        try
        {
            var create = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/visit-logs", new CreateVisitLogRequest
            {
                TimelineEntryId = entryId,
                VisitedAt = visitedAt,
                ActualCost = 12.50m,
                CurrencyCode = "USD",
                Rating = 5,
                Note = "  excellent  "
            });
            var list = await client.GetFromJsonAsync<PagedResponse<VisitLogResponse>>(
                $"/api/v1/trips/{tripId}/visit-logs?tripDestinationId={destinationId}", Json);
            var summary = await client.GetFromJsonAsync<TripSummaryResponse>(
                $"/api/v1/trips/{tripId}/summary", Json);

            create.StatusCode.Should().Be(HttpStatusCode.Created);
            list!.Data.Should().ContainSingle(x => x.TimelineEntryId == entryId && x.Note == "excellent");
            summary!.TotalVisitCount.Should().Be(1);
            summary.VisitedPlannedEntryCount.Should().Be(1);
            summary.VisitCompletionPercentage.Should().Be(100m);
            summary.ActualVisitCost.Should().Be(12.50m);

            await SoftDeleteTimelineEntryAsync(entryId);
            await SeedSoftDeletedUnvisitedEntryAsync(tripId, destinationId);
            var summaryAfterEntryDeletion = await client.GetAsync($"/api/v1/trips/{tripId}/summary");
            var preservedSummary = await summaryAfterEntryDeletion.Content
                .ReadFromJsonAsync<TripSummaryResponse>(Json);
            summaryAfterEntryDeletion.StatusCode.Should().Be(HttpStatusCode.OK);
            preservedSummary!.TotalVisitCount.Should().Be(1);
            preservedSummary.PlannedVisitableEntryCount.Should().Be(1);

            await ClearDestinationTimezoneAsync(destinationId);
            var unavailable = await client.GetAsync($"/api/v1/trips/{tripId}/visit-logs");
            var error = await unavailable.Content.ReadFromJsonAsync<ErrorResponse>(Json);
            unavailable.StatusCode.Should().Be(HttpStatusCode.Conflict);
            error!.Code.Should().Be("TIMEZONE_UNAVAILABLE");
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    [Fact]
    public async Task VisitLogFilters_OffsetlessDateIsTreatedAsUtc()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (tripId, _, _, visitedAt) = await SeedActiveTripAsync();
        try
        {
            var response = await client.GetAsync(
                $"/api/v1/trips/{tripId}/visit-logs?visitedFrom={visitedAt:yyyy-MM-ddTHH:mm:ss}");
            var invalidResponse = await client.GetAsync(
                $"/api/v1/trips/{tripId}/visit-logs?visitedFrom=not-a-date");
            var invalidError = await invalidResponse.Content.ReadFromJsonAsync<ErrorResponse>(Json);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            invalidError!.Code.Should().Be("INVALID_DATE_FILTER");
        }
        finally
        {
            await DeleteSeededTripAsync(tripId);
        }
    }

    private Task<HttpClient> CreateAuthenticatedClientAsync() => CreateAuthenticatedClientAsync(
        TestDatabaseSeeder.TestUserEmail,
        TestDatabaseSeeder.TestUserPassword);

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthenticationResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private async Task<(Guid TripId, Guid DestinationId, Guid EntryId, DateTime VisitedAt)> SeedActiveTripAsync(
        int localDayOffset = 0)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ownerId = await db.Users.Where(x => x.Email == TestDatabaseSeeder.TestUserEmail)
            .Select(x => x.Id).SingleAsync();
        var visitedAt = DateTime.UtcNow.AddMinutes(-1);
        var localDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(visitedAt, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")))
            .AddDays(localDayOffset);
        var trip = new Trip
        {
            OwnerId = ownerId,
            Title = $"Live trip {Guid.NewGuid():N}",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 1,
            BudgetTier = BudgetTier.Standard,
            BaseCurrencyCode = "USD"
        };
        var destination = new TripDestination(localDate, localDate, "Istanbul", "Turkey", 1)
        {
            TripId = trip.Id,
            Timezone = "Europe/Istanbul",
            Latitude = 41.0082,
            Longitude = 28.9784
        };
        trip.Destinations.Add(destination);
        trip.RecalculateFromDestinations();
        var entry = TimelineEntry.CreateCustomEventEntry(
            trip.Id, destination.Id, 1, 1, "Museum", new TimeOnly(10, 0), 60,
            price: 5m, customLatitude: 41.01, customLongitude: 28.98);
        db.Trips.Add(trip);
        db.TimelineEntries.Add(entry);
        await db.SaveChangesAsync();
        return (trip.Id, destination.Id, entry.Id, visitedAt);
    }

    private async Task DeleteSeededTripAsync(Guid tripId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.PlaceVisitLogs.IgnoreQueryFilters().Where(x => x.TripId == tripId).ExecuteDeleteAsync();
        await db.TimelineEntries.IgnoreQueryFilters().Where(x => x.TripId == tripId).ExecuteDeleteAsync();
        await db.TripDestinations.IgnoreQueryFilters().Where(x => x.TripId == tripId).ExecuteDeleteAsync();
        await db.Trips.IgnoreQueryFilters().Where(x => x.Id == tripId).ExecuteDeleteAsync();
    }

    private async Task<Guid[]> SeedNearbyPlacesAsync(Guid tripId, Guid destinationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Trips.Where(x => x.Id == tripId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, TripStatus.Published));

        var eligible = CreatePlace("Nearby cafe", PlaceCategory.Cafe, 41.0083, 28.9785);
        var excludedByTimeline = CreatePlace("Planned cafe", PlaceCategory.Cafe, 41.0084, 28.9786);
        var excludedHotel = CreatePlace("Nearby hotel", PlaceCategory.Hotel, 41.0085, 28.9787);
        var outsideRadius = CreatePlace("Far cafe", PlaceCategory.Cafe, 41.10, 28.9784);
        db.Places.AddRange(eligible, excludedByTimeline, excludedHotel, outsideRadius);
        db.TimelineEntries.Add(TimelineEntry.CreatePlaceEntry(
            tripId, destinationId, 1, 2, excludedByTimeline.Id));
        await db.SaveChangesAsync();
        return [eligible.Id, excludedByTimeline.Id, excludedHotel.Id, outsideRadius.Id];
    }

    private static Place CreatePlace(
        string name,
        PlaceCategory category,
        double latitude,
        double longitude) => new()
    {
        Name = name,
        Category = category,
        Latitude = latitude,
        Longitude = longitude,
        City = "Istanbul",
        Country = "Turkey",
        CurrencyCode = "USD",
        IsActive = true,
        BudgetTiers = [BudgetTier.Standard]
    };

    private async Task DeletePlacesAsync(IEnumerable<Guid> placeIds)
    {
        var ids = placeIds.ToArray();
        if (ids.Length == 0)
            return;
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Places.Where(place => ids.Contains(place.Id)).ExecuteDeleteAsync();
    }

    private async Task ClearDestinationTimezoneAsync(Guid destinationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.TripDestinations.Where(x => x.Id == destinationId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Timezone, (string?)null));
    }

    private async Task SetTripStatusAsync(Guid tripId, TripStatus status)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Trips.Where(x => x.Id == tripId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, status));
    }

    private static Task<HttpResponseMessage> SearchNearbyAsync(
        HttpClient client,
        Guid tripId,
        Guid destinationId,
        double latitude,
        double longitude) => client.PostAsJsonAsync(
        $"/api/v1/trips/{tripId}/nearby-places/search",
        new SearchNearbyPlacesRequest
        {
            TripDestinationId = destinationId,
            Latitude = latitude,
            Longitude = longitude,
            RadiusKm = 1,
            CategoryGroup = NearbyPlaceCategoryGroup.All
        });

    private async Task SoftDeleteTimelineEntryAsync(Guid timelineEntryId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.TimelineEntries.Where(x => x.Id == timelineEntryId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.DeletedAt, DateTime.UtcNow));
    }

    private async Task SeedSoftDeletedUnvisitedEntryAsync(Guid tripId, Guid destinationId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = TimelineEntry.CreateCustomEventEntry(
            tripId, destinationId, 1, 2, "Deleted unvisited stop", new TimeOnly(12, 0), 30);
        entry.DeletedAt = DateTime.UtcNow;
        db.TimelineEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    private static async Task DeleteStaleSeededTripsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var tripIds = db.Trips.IgnoreQueryFilters()
            .Where(x => EF.Functions.Like(x.Title, "Live trip %"))
            .Select(x => x.Id);
        await db.PlaceVisitLogs.IgnoreQueryFilters().Where(x => tripIds.Contains(x.TripId)).ExecuteDeleteAsync();
        await db.TimelineEntries.IgnoreQueryFilters().Where(x => tripIds.Contains(x.TripId)).ExecuteDeleteAsync();
        await db.TripDestinations.IgnoreQueryFilters().Where(x => tripIds.Contains(x.TripId)).ExecuteDeleteAsync();
        await db.Trips.IgnoreQueryFilters().Where(x => EF.Functions.Like(x.Title, "Live trip %")).ExecuteDeleteAsync();
    }
}
