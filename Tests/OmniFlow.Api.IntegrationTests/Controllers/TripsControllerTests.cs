using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        var createRequest = new CreateTripRequest
        {
            Title = "Completion Smoke Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await authClient.GetAsync("/api/v1/trips");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetMyTripsViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Data.Should().NotBeEmpty();
        result.Data.Should().OnlyContain(trip =>
            trip.CompletionPercentage >= 0 && trip.CompletionPercentage <= 100);
    }

    // ── GET Trip By Id ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_PublishedTripWithoutToken_Returns200AndAnonymousFlagsAreNull()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndPublishTripAsync(authClient);

        var response = await _client.GetAsync($"/api/v1/trips/{tripId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var trip = JsonSerializer.Deserialize<TripResponse>(
            await response.Content.ReadAsStringAsync(),
            _json);

        trip.Should().NotBeNull();
        trip!.IsSaved.Should().BeNull();
        trip.IsUpvoted.Should().BeNull();
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var response = await authClient.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AsOwner_DoesNotIncrementViewCount()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTripAsync(authClient, "Owner View Count Trip");

        var response = await authClient.GetAsync($"/api/v1/trips/{tripId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var trip = JsonSerializer.Deserialize<TripResponse>(await response.Content.ReadAsStringAsync(), _json);
        trip.Should().NotBeNull();
        trip!.ViewCount.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).ViewCount.Should().Be(0);
    }

    [Fact]
    public async Task GetById_AsDifferentUserForPublishedTrip_IncrementsViewCount()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var tripId = await CreateAndPublishTripAsync(ownerClient);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var response = await visitorClient.GetAsync($"/api/v1/trips/{tripId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var trip = JsonSerializer.Deserialize<TripResponse>(await response.Content.ReadAsStringAsync(), _json);
        trip.Should().NotBeNull();
        trip!.ViewCount.Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).ViewCount.Should().Be(1);
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
        result.Destinations[0].Latitude.Should().Be(41.0082);
        result.Destinations[0].Longitude.Should().Be(28.9784);
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

    [Fact]
    public async Task UploadCoverPhoto_WithoutToken_Returns401()
    {
        using var client = CreateClientWithFakeBlobService();
        using var content = CreateMultipart("file", "cover.jpg", "image/jpeg", "fake image");

        var response = await client.PostAsync($"/api/v1/Trips/{Guid.NewGuid()}/cover-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadCoverPhoto_AsOwner_ReturnsUrlAndUpdatesTrip()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        using var authClient = CreateClientWithFakeBlobService(token);
        var tripId = await CreateTripAsync(authClient, "Cover Photo Trip");
        using var content = CreateMultipart("file", "cover.jpg", "image/jpeg", "fake image");

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/cover-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = JsonSerializer.Deserialize<UploadTripCoverPhotoResponse>(
            await response.Content.ReadAsStringAsync(),
            _json);

        result.Should().NotBeNull();
        result!.CoverPhotoUrl.Should().Be("https://blob.test/trip-cover-photos/cover.jpg");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).CoverPhotoUrl.Should().Be(result.CoverPhotoUrl);
    }

    [Fact]
    public async Task UploadCoverPhoto_AsNonOwner_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        using var ownerClient = CreateClientWithFakeBlobService(ownerToken);
        var tripId = await CreateTripAsync(ownerClient, "Forbidden Cover Photo Trip");

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        using var visitorClient = CreateClientWithFakeBlobService(visitorToken);
        using var content = CreateMultipart("file", "cover.jpg", "image/jpeg", "fake image");

        var response = await visitorClient.PostAsync($"/api/v1/Trips/{tripId}/cover-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).CoverPhotoUrl.Should().BeNull();
    }

    [Fact]
    public async Task UploadCoverPhoto_WithUnsupportedContentType_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        using var authClient = CreateClientWithFakeBlobService(token);
        var tripId = await CreateTripAsync(authClient, "Unsupported Cover Photo Trip");
        using var content = CreateMultipart("file", "notes.txt", "text/plain", "not an image");

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/cover-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadCoverPhoto_WithEmptyFile_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        using var authClient = CreateClientWithFakeBlobService(token);
        var tripId = await CreateTripAsync(authClient, "Empty Cover Photo Trip");
        using var content = CreateMultipart("file", "empty.jpg", "image/jpeg", string.Empty);

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/cover-photo", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── DELETE Trip ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChecklist_ForPublishedTripWithoutToken_ReturnsCurrentItems()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Published Checklist Trip");
        await AddTimelineEntryAsync(trip.TripId, trip.Destinations[0].Id);

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await _client.GetAsync($"/api/v1/Trips/{trip.TripId}/checklist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<TripChecklistStatusResponse>(
            await response.Content.ReadAsStringAsync(),
            _json);

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(4);
        result.Items.Should().Contain(i =>
            i.ItemKey == $"flight-leg:{trip.Destinations[0].Id:D}:{trip.Destinations[1].Id:D}" &&
            !i.IsConfirmed &&
            i.ConfirmedAt == null);
    }

    [Fact]
    public async Task GetChecklist_ForDraftTripWithoutToken_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Private Checklist Trip");

        var response = await _client.GetAsync($"/api/v1/Trips/{trip.TripId}/checklist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChecklist_AsOwnerForArchivedTrip_Returns200()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Archived Checklist Trip");
        await AddTimelineEntryAsync(trip.TripId, trip.Destinations[0].Id);

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archiveResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await authClient.GetAsync($"/api/v1/Trips/{trip.TripId}/checklist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task B010_ReadEndpoints_ForPublishedTrip_AllowAnonymous()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "B010 Published Visibility Trip");
        await AddTimelineEntryAsync(trip.TripId, trip.Destinations[0].Id);

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var responses = await GetReadEndpointResponsesAsync(_client, trip.TripId, trip.Destinations[0].Id);

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task B010_ReadEndpoints_ForDraftTrip_Return404ForAnonymousAndNonOwner()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "B010 Draft Visibility Trip");

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var anonymousResponses = await GetReadEndpointResponsesAsync(_client, trip.TripId, trip.Destinations[0].Id);
        var visitorResponses = await GetReadEndpointResponsesAsync(visitorClient, trip.TripId, trip.Destinations[0].Id);
        var ownerResponses = await GetReadEndpointResponsesAsync(authClient, trip.TripId, trip.Destinations[0].Id);

        anonymousResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        visitorResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        ownerResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task B010_ReadEndpoints_ForArchivedTrip_Return404ForAnonymousAndNonOwner()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "B010 Archived Visibility Trip");
        await AddTimelineEntryAsync(trip.TripId, trip.Destinations[0].Id);

        var publishResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archiveResponse = await authClient.PostAsync($"/api/v1/trips/{trip.TripId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var anonymousResponses = await GetReadEndpointResponsesAsync(_client, trip.TripId, trip.Destinations[0].Id);
        var visitorResponses = await GetReadEndpointResponsesAsync(visitorClient, trip.TripId, trip.Destinations[0].Id);
        var ownerResponses = await GetReadEndpointResponsesAsync(authClient, trip.TripId, trip.Destinations[0].Id);

        anonymousResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        visitorResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        ownerResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task B010_GetDestinations_ForPrivateTripWithoutDestinations_Returns404ForAnonymousAndNonOwner()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTripAsync(authClient, "B010 Empty Destination Private Trip");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var destinations = db.TripDestinations.Where(destination => destination.TripId == tripId).ToList();
            db.TripDestinations.RemoveRange(destinations);
            await db.SaveChangesAsync();
        }

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var anonymousResponse = await _client.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var visitorResponse = await visitorClient.GetAsync($"/api/v1/trips/{tripId}/destinations");
        var ownerResponse = await authClient.GetAsync($"/api/v1/trips/{tripId}/destinations");

        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        visitorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ToggleChecklistItem_AsOwner_ConfirmsAndClearsItem()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Toggle Checklist Trip");
        var itemKey = $"hotel-night:{trip.Destinations[0].Id:D}:1";

        var confirmResponse = await authClient.PutAsJsonAsync(
            $"/api/v1/Trips/{trip.TripId}/checklist/{Uri.EscapeDataString(itemKey)}",
            new ToggleChecklistItemRequest { IsConfirmed = true });

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var confirmedResult = await GetChecklistAsync(authClient, trip.TripId);
        var confirmedItem = confirmedResult.Items.Single(i => i.ItemKey == itemKey);
        confirmedItem.IsConfirmed.Should().BeTrue();
        confirmedItem.ConfirmedAt.Should().NotBeNull();

        var clearResponse = await authClient.PutAsJsonAsync(
            $"/api/v1/Trips/{trip.TripId}/checklist/{Uri.EscapeDataString(itemKey)}",
            new ToggleChecklistItemRequest { IsConfirmed = false });

        clearResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var clearedResult = await GetChecklistAsync(authClient, trip.TripId);
        var clearedItem = clearedResult.Items.Single(i => i.ItemKey == itemKey);
        clearedItem.IsConfirmed.Should().BeFalse();
        clearedItem.ConfirmedAt.Should().BeNull();
    }

    [Fact]
    public async Task ToggleChecklistItem_AsNonOwner_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var trip = await CreateChecklistTripAsync(ownerClient, "Forbidden Checklist Trip");
        var itemKey = $"hotel-night:{trip.Destinations[0].Id:D}:1";

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var response = await visitorClient.PutAsJsonAsync(
            $"/api/v1/Trips/{trip.TripId}/checklist/{Uri.EscapeDataString(itemKey)}",
            new ToggleChecklistItemRequest { IsConfirmed = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ToggleChecklistItem_WithInvalidItemKey_Returns404()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Invalid Checklist Trip");

        var response = await authClient.PutAsJsonAsync(
            $"/api/v1/Trips/{trip.TripId}/checklist/{Uri.EscapeDataString("hotel-night:invalid:1")}",
            new ToggleChecklistItemRequest { IsConfirmed = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetChecklist_WithStaleConfirmation_FiltersStaleItem()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Stale Checklist Trip");
        const string staleItemKey = "hotel-night:00000000-0000-0000-0000-000000000000:9";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            db.TripChecklistConfirmations.Add(new TripChecklistConfirmation
            {
                TripId = trip.TripId,
                ItemKey = staleItemKey,
                IsConfirmed = true,
                ConfirmedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var result = await GetChecklistAsync(authClient, trip.TripId);

        result.Items.Should().NotContain(i => i.ItemKey == staleItemKey);
        result.Items.Should().OnlyContain(i => i.ItemKey != staleItemKey);
    }

    [Fact]
    public async Task DeleteDestination_RemovesRelatedChecklistConfirmations()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var trip = await CreateChecklistTripAsync(authClient, "Cleanup Checklist Trip");
        var destinationToDelete = trip.Destinations[1];
        var itemKey = $"flight-leg:{trip.Destinations[0].Id:D}:{destinationToDelete.Id:D}";

        var toggleResponse = await authClient.PutAsJsonAsync(
            $"/api/v1/Trips/{trip.TripId}/checklist/{Uri.EscapeDataString(itemKey)}",
            new ToggleChecklistItemRequest { IsConfirmed = true });
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var deleteResponse = await authClient.DeleteAsync(
            $"/api/v1/trips/{trip.TripId}/destinations/{destinationToDelete.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.TripChecklistConfirmations
            .Any(c => c.TripId == trip.TripId && c.ItemKey == itemKey)
            .Should().BeFalse();
    }

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

    [Fact]
    public async Task Unarchive_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/Trips/{Guid.NewGuid()}/unarchive", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unarchive_AsOwnerForArchivedTrip_Returns204AndPublishesTrip()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndArchiveTripAsync(authClient);

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unarchive", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).Status.Should().Be(TripStatus.Published);
    }

    [Fact]
    public async Task Unarchive_AsNonOwner_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var tripId = await CreateAndArchiveTripAsync(ownerClient);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var response = await visitorClient.PostAsync($"/api/v1/Trips/{tripId}/unarchive", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unarchive_ForDraftTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTripAsync(authClient, "Draft Unarchive Trip");

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unarchive", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unarchive_ForPublishedTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndPublishTripAsync(authClient);

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unarchive", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unarchive_AfterSuccess_AllowsAnonymousRead()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndArchiveTripAsync(authClient);

        var unarchiveResponse = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unarchive", null);
        unarchiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/Trips/{tripId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST Unpublish Trip ────────────────────────────────────────────────────────

    [Fact]
    public async Task Unpublish_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync($"/api/v1/Trips/{Guid.NewGuid()}/unpublish", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unpublish_AsOwnerForPublishedTrip_Returns204AndMovesTripToDraft()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndPublishTripAsync(authClient);

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.Trips.Single(t => t.Id == tripId).Status.Should().Be(TripStatus.Draft);
    }

    [Fact]
    public async Task Unpublish_AsNonOwner_Returns403()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var tripId = await CreateAndPublishTripAsync(ownerClient);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);

        var response = await visitorClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unpublish_ForDraftTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateTripAsync(authClient, "Draft Unpublish Trip");

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unpublish_ForArchivedTrip_Returns400()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndArchiveTripAsync(authClient);

        var response = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unpublish_AfterSuccess_HidesTripFromAnonymousAndNonOwnerButOwnerCanRead()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var tripId = await CreateAndPublishTripAsync(ownerClient);

        var unpublishResponse = await ownerClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymousResponse = await _client.GetAsync($"/api/v1/Trips/{tripId}");
        anonymousResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);
        var visitorResponse = await visitorClient.GetAsync($"/api/v1/Trips/{tripId}");
        visitorResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownerResponse = await ownerClient.GetAsync($"/api/v1/Trips/{tripId}");
        ownerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Unpublish_PreservesEngagementCountsAndSavedTrips()
    {
        var ownerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var ownerClient = CreateAuthenticatedClient(ownerToken);
        var tripId = await CreateAndPublishTripAsync(ownerClient);

        var visitorToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
        var visitorClient = CreateAuthenticatedClient(visitorToken);
        var saveResponse = await visitorClient.PostAsync($"/api/v1/Trips/{tripId}/save", null);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var trip = db.Trips.Single(t => t.Id == tripId);
            trip.UpvoteCount = 7;
            trip.ForkCount = 4;
            await db.SaveChangesAsync();
        }

        var response = await ownerClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var unpublishedTrip = verifyDb.Trips.Single(t => t.Id == tripId);
        unpublishedTrip.Status.Should().Be(TripStatus.Draft);
        unpublishedTrip.UpvoteCount.Should().Be(7);
        unpublishedTrip.ForkCount.Should().Be(4);
        verifyDb.SavedTrips.Count(savedTrip => savedTrip.TripId == tripId).Should().Be(1);
    }

    [Fact]
    public async Task Unpublish_ThenPublishAgain_AllowsAnonymousRead()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);
        var tripId = await CreateAndPublishTripAsync(authClient);

        var unpublishResponse = await authClient.PostAsync($"/api/v1/Trips/{tripId}/unpublish", null);
        unpublishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publishResponse = await authClient.PostAsync($"/api/v1/Trips/{tripId}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/Trips/{tripId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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

        using (var setupScope = _factory.Services.CreateScope())
        {
            var setupDb = setupScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var originalDestinationId = setupDb.TripDestinations
                .Where(d => d.TripId == tripId && d.DeletedAt == null)
                .OrderBy(d => d.OrderIndex)
                .Select(d => d.Id)
                .First();
            var originalEntry = setupDb.TimelineEntries
                .First(e => e.TripId == tripId && e.DeletedAt == null);
            originalEntry.SetPlanningSlotKey($"hotel-night:{originalDestinationId:D}:1");
            await setupDb.SaveChangesAsync();
        }

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
        originalTimelineEntries.Should().Contain(e => e.PlanningSlotKey != null);
        forkedTimelineEntries.Should().OnlyContain(e => e.PlanningSlotKey == null);
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

    private static async Task<List<HttpResponseMessage>> GetReadEndpointResponsesAsync(
        HttpClient client,
        Guid tripId,
        Guid destinationId)
    {
        return
        [
            await client.GetAsync($"/api/v1/Trips/{tripId}"),
            await client.GetAsync($"/api/v1/Trips/{tripId}/budget-summary"),
            await client.GetAsync($"/api/v1/trips/{tripId}/timeline"),
            await client.GetAsync($"/api/v1/trips/{tripId}/destinations"),
            await client.GetAsync($"/api/v1/Trips/{tripId}/checklist"),
            await client.GetAsync($"/api/v1/Trips/{tripId}/recommend-places?destinationId={destinationId}")
        ];
    }

    private async Task<CreateTripWizardResponse> CreateChecklistTripAsync(HttpClient authClient, string title)
    {
        var createRequest = new CreateTripWizardRequest
        {
            Title = title,
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelCompanion = TravelCompanion.Couple,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural },
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.PublicTransport,
            Destinations =
            [
                new OmniFlow.Application.DTOs.TripDestinations.CreateTripDestinationRequest
                {
                    City = "Paris",
                    Country = "France",
                    ArrivalDate = new DateOnly(2026, 8, 1),
                    DepartureDate = new DateOnly(2026, 8, 3),
                    OrderIndex = 1
                },
                new OmniFlow.Application.DTOs.TripDestinations.CreateTripDestinationRequest
                {
                    City = "Rome",
                    Country = "Italy",
                    ArrivalDate = new DateOnly(2026, 8, 3),
                    DepartureDate = new DateOnly(2026, 8, 4),
                    OrderIndex = 2
                }
            ]
        };

        var response = await authClient.PostAsJsonAsync("/api/v1/trips/wizard", createRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = JsonSerializer.Deserialize<CreateTripWizardResponse>(
            await response.Content.ReadAsStringAsync(),
            _json);

        result.Should().NotBeNull();
        return result!;
    }

    private async Task AddTimelineEntryAsync(Guid tripId, Guid destinationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        db.TimelineEntries.Add(TimelineEntry.CreateCustomEventEntry(
            tripId,
            destinationId,
            1,
            1000,
            "Checklist publish entry",
            new TimeOnly(10, 0),
            60));

        await db.SaveChangesAsync();
    }

    private static async Task<TripChecklistStatusResponse> GetChecklistAsync(HttpClient client, Guid tripId)
    {
        var response = await client.GetAsync($"/api/v1/Trips/{tripId}/checklist");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = JsonSerializer.Deserialize<TripChecklistStatusResponse>(
            await response.Content.ReadAsStringAsync(),
            _json);

        result.Should().NotBeNull();
        return result!;
    }

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

    private async Task<Guid> CreateAndArchiveTripAsync(HttpClient authClient)
    {
        var tripId = await CreateAndPublishTripAsync(authClient);

        var archiveResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        return tripId;
    }

    private async Task<Guid> CreateTripAsync(HttpClient authClient, string title)
    {
        var createRequest = new CreateTripRequest
        {
            Title = title,
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 12, 5),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResult = JsonSerializer.Deserialize<CreateTripWizardResponse>(
            await createResponse.Content.ReadAsStringAsync(),
            _json);

        return createResult!.TripId;
    }

    private HttpClient CreateClientWithFakeBlobService(string? token = null)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBlobService>();
                services.AddScoped<IBlobService, FakeBlobService>();
            });
        }).CreateClient();

        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static MultipartFormDataContent CreateMultipart(
        string name,
        string fileName,
        string contentType,
        string body)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, name, fileName);
        return content;
    }

    private sealed class FakeBlobService : IBlobService
    {
        public Task<string> UploadAsync(
            Stream stream,
            string contentType,
            string? originalFileName,
            string? folder = null,
            CancellationToken cancellationToken = default)
        {
            var safeFolder = string.IsNullOrWhiteSpace(folder) ? "root" : folder.Trim('/');
            return Task.FromResult($"https://blob.test/{safeFolder}/{originalFileName}");
        }
    }
}
