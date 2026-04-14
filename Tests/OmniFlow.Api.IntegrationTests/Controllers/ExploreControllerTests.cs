using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Queries.ExploreTrips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class ExploreControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public ExploreControllerTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();

		using var scope = factory.Services.CreateScope();
		TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
	}

	[Fact]
	public async Task Explore_WithSearchTerm_ReturnsMatchingDestinationsRoutesAndCreators()
	{
		await ClearTripDataAsync();

		var suffix = Guid.NewGuid().ToString("N")[..8];
		var routeTerm = $"route{suffix}";
		var cityTerm = $"city{suffix}";
		var creatorTerm = $"creator{suffix}";

		var routeOwner = await CreateOwnerAsync($"route{suffix}");
		var cityOwner = await CreateOwnerAsync($"city{suffix}");
		var creatorOwner = await CreateOwnerAsync($"creator{suffix}", $"featured_{creatorTerm}");
		var otherOwner = await CreateOwnerAsync($"other{suffix}");

		var routeTrip = CreateTrip(routeOwner.Id, $"Aurora {routeTerm}", TripStatus.Published, DateTime.UtcNow, score: 300);
		var cityTrip = CreateTrip(cityOwner.Id, $"Coastal Escape {suffix}", TripStatus.Published, DateTime.UtcNow, score: 200);
		cityTrip.City = cityTerm;
		var creatorTrip = CreateTrip(creatorOwner.Id, $"Creator Escape {suffix}", TripStatus.Published, DateTime.UtcNow, score: 100);
		var nonMatchingTrip = CreateTrip(otherOwner.Id, $"Plain Escape {suffix}", TripStatus.Published, DateTime.UtcNow, score: 900);
		var draftMatchingTrip = CreateTrip(otherOwner.Id, $"Draft {routeTerm}", TripStatus.Draft, DateTime.UtcNow, score: 1_000);

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			db.Trips.AddRange(routeTrip, cityTrip, creatorTrip, nonMatchingTrip, draftMatchingTrip);
			await db.SaveChangesAsync();
		}

		var routeResult = await GetExploreTripsAsync(routeTerm);
		routeResult.Data.Should().ContainSingle(x => x.Id == routeTrip.Id);
		routeResult.Data.Should().NotContain(x => x.Id == draftMatchingTrip.Id || x.Id == nonMatchingTrip.Id);

		var cityResult = await GetExploreTripsAsync(cityTerm);
		cityResult.Data.Should().ContainSingle(x => x.Id == cityTrip.Id);
		cityResult.Data.Should().NotContain(x => x.Id == nonMatchingTrip.Id);

		var creatorResult = await GetExploreTripsAsync(creatorTerm);
		creatorResult.Data.Should().ContainSingle(x => x.Id == creatorTrip.Id);
		creatorResult.Data.Should().NotContain(x => x.Id == nonMatchingTrip.Id);
	}

	[Fact]
	public async Task GetFeatured_WithoutToken_ReturnsRecentPublishedTripsOnly()
	{
		var suffix = Guid.NewGuid().ToString("N")[..8];
		var owner = await CreateOwnerAsync(suffix);
		var recentCreatedAt = DateTime.UtcNow.AddDays(-1);
		var oldCreatedAt = DateTime.UtcNow.AddDays(-14);
		var recentPublishedTrip = CreateTrip(owner.Id, $"Featured Recent {suffix}", TripStatus.Published, recentCreatedAt, score: 900);
		var oldPublishedTrip = CreateTrip(owner.Id, $"Featured Old {suffix}", TripStatus.Published, oldCreatedAt, score: 10_000);
		var draftTrip = CreateTrip(owner.Id, $"Featured Draft {suffix}", TripStatus.Draft, recentCreatedAt, score: 10_000);
		var archivedTrip = CreateTrip(owner.Id, $"Featured Archived {suffix}", TripStatus.Archived, recentCreatedAt, score: 10_000);

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			db.Trips.AddRange(recentPublishedTrip, oldPublishedTrip, draftTrip, archivedTrip);
			await db.SaveChangesAsync();

			recentPublishedTrip.CreatedAt = recentCreatedAt;
			oldPublishedTrip.CreatedAt = oldCreatedAt;
			draftTrip.CreatedAt = recentCreatedAt;
			archivedTrip.CreatedAt = recentCreatedAt;
			await db.SaveChangesAsync();
		}

		var response = await _client.GetAsync("/api/v1/explore/featured?limit=6");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var result = await DeserializeFeaturedTripsAsync(response);
		result.Should().Contain(x => x.Id == recentPublishedTrip.Id);
		result.Should().NotContain(x => x.Id == oldPublishedTrip.Id);
		result.Should().NotContain(x => x.Id == draftTrip.Id);
		result.Should().NotContain(x => x.Id == archivedTrip.Id);
	}

	[Fact]
	public async Task GetFeatured_WhenNoRecentPublishedTrips_FallsBackToOlderPublishedTrips()
	{
		await ClearTripDataAsync();

		var suffix = Guid.NewGuid().ToString("N")[..8];
		var owner = await CreateOwnerAsync(suffix);
		var oldCreatedAt = DateTime.UtcNow.AddDays(-14);
		var oldPublishedTrip = CreateTrip(owner.Id, $"Featured Fallback {suffix}", TripStatus.Published, oldCreatedAt, score: 500);
		var oldDraftTrip = CreateTrip(owner.Id, $"Featured Fallback Draft {suffix}", TripStatus.Draft, oldCreatedAt, score: 900);

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			db.Trips.AddRange(oldPublishedTrip, oldDraftTrip);
			await db.SaveChangesAsync();

			oldPublishedTrip.CreatedAt = oldCreatedAt;
			oldDraftTrip.CreatedAt = oldCreatedAt;
			await db.SaveChangesAsync();
		}

		var response = await _client.GetAsync("/api/v1/explore/featured?limit=6");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var result = await DeserializeFeaturedTripsAsync(response);
		result.Should().ContainSingle(x => x.Id == oldPublishedTrip.Id);
		result.Should().NotContain(x => x.Id == oldDraftTrip.Id);
	}

	private async Task<User> CreateOwnerAsync(string suffix, string? username = null)
	{
		var owner = new User
		{
			Id = Guid.NewGuid(),
			Username = username ?? $"featured_{suffix}",
			Email = $"{username ?? $"featured_{suffix}"}@omniflow.com",
			KarmaScore = 100,
			Role = Roles.Traveler,
			ProfilePhotoUrl = "https://cdn.example.com/featured-owner.jpg"
		};

		using var scope = _factory.Services.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var appUser = new ApplicationUser
		{
			Id = owner.Id,
			UserName = owner.Username,
			Email = owner.Email,
			EmailConfirmed = true
		};

		var result = await userManager.CreateAsync(appUser, "TestUser123!");
		result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.Description)));

		db.Users.Add(owner);
		await db.SaveChangesAsync();

		return owner;
	}

	private static Trip CreateTrip(Guid ownerId, string title, TripStatus status, DateTime createdAt, int score)
	{
		return new Trip
		{
			Id = Guid.NewGuid(),
			OwnerId = ownerId,
			Title = title,
			Status = status,
			City = "Antalya",
			Country = "Turkey",
			StartDate = new DateOnly(2026, 7, 1),
			EndDate = new DateOnly(2026, 7, 7),
			PersonCount = 2,
			BudgetTier = BudgetTier.Standard,
			TravelStyle = TravelStyle.Adventure,
			ForkCount = score,
			UpvoteCount = score,
			ViewCount = score,
			PopularityScore = score,
			CreatedAt = createdAt,
			UpdatedAt = createdAt
		};
	}

	private static async Task<List<FeaturedTripResponse>> DeserializeFeaturedTripsAsync(HttpResponseMessage response)
	{
		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<List<FeaturedTripResponse>>(body, _json) ?? new List<FeaturedTripResponse>();
	}

	private async Task<ExploreTripsViewModel> GetExploreTripsAsync(string searchTerm)
	{
		var response = await _client.GetAsync($"/api/v1/explore?searchTerm={Uri.EscapeDataString(searchTerm)}&pageSize=10");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<ExploreTripsViewModel>(body, _json)!;
	}

	private async Task ClearTripDataAsync()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

		db.CommentUpvotes.RemoveRange(db.CommentUpvotes);
		db.PostUpvotes.RemoveRange(db.PostUpvotes);
		db.TipUpvotes.RemoveRange(db.TipUpvotes);
		db.Comments.RemoveRange(db.Comments);
		db.Posts.RemoveRange(db.Posts);
		db.CommunityTips.RemoveRange(db.CommunityTips);
		db.SavedTrips.RemoveRange(db.SavedTrips);
		db.TripUpvotes.RemoveRange(db.TripUpvotes);
		db.Stops.RemoveRange(db.Stops);
		db.Flights.RemoveRange(db.Flights);
		db.Hotels.RemoveRange(db.Hotels);
		db.Trips.RemoveRange(db.Trips);

		await db.SaveChangesAsync();
	}
}
