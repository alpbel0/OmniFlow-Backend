using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public UsersControllerTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();

		using var scope = factory.Services.CreateScope();
		TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
	}

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

	private async Task<Guid> GetUserIdAsync(string email)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		return db.Users.Single(x => x.Email == email).Id;
	}

	private async Task EnsureBlockRelationAsync(Guid blockerId, Guid blockedUserId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

		if (!db.Blocks.Any(x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId))
		{
			db.Blocks.Add(new Block { BlockerId = blockerId, BlockedUserId = blockedUserId });
			await db.SaveChangesAsync();
		}
	}

	[Fact]
	public async Task GetTopContributors_WithoutToken_Returns200AndExcludesSuspendedUsers()
	{
		var suffix = Guid.NewGuid().ToString("N")[..8];
		var activeUser = await CreateUserAsync($"000_leader_{suffix}", int.MaxValue, isSuspended: false, "https://cdn.example.com/leader.jpg");
		var suspendedUser = await CreateUserAsync($"zzz_suspended_{suffix}", 1_000_000, isSuspended: true);

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			var trip = new Trip
			{
				Id = Guid.NewGuid(),
				OwnerId = activeUser.Id,
				Title = $"Leaderboard Trip {suffix}",
				Status = TripStatus.Published,
				Origin = "Antalya",
				OriginCountry = "Turkey",
				PersonCount = 2,
				BudgetTier = BudgetTier.Standard,
				TravelCompanion = TravelCompanion.Friends,
				Tempo = Tempo.Moderate,
				TransportPreference = TransportPreference.PublicTransport,
				TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
			};

			trip.Destinations.Add(new TripDestination(
				arrivalDate: new DateOnly(2030, 1, 10),
				departureDate: new DateOnly(2030, 1, 13),
				city: "Paris",
				country: "France",
				orderIndex: 1));
			trip.RecalculateFromDestinations();

			db.Trips.Add(trip);
			await db.SaveChangesAsync();
		}

		var response = await _client.GetAsync("/api/v1/users/top-contributors?limit=5");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<List<TopContributorResponse>>(body, _json);

		result.Should().NotBeNull();
		result!.Should().Contain(x =>
			x.Id == activeUser.Id &&
			x.Username == activeUser.Username &&
			x.KarmaScore == activeUser.KarmaScore &&
			x.ProfilePhotoUrl == activeUser.ProfilePhotoUrl &&
			x.TripCount == 1);
		result.Should().NotContain(x => x.Id == suspendedUser.Id);
	}

	[Fact]
	public async Task GetByUsername_WithoutToken_Returns401()
	{
		var response = await _client.GetAsync($"/api/v1/users/{TestDatabaseSeeder.TestUserUsername}");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetByUsername_WithValidToken_Returns200()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.GetAsync($"/api/v1/users/{TestDatabaseSeeder.TestUserUsername}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<UserProfileResponse>(body, _json);

		result.Should().NotBeNull();
		result!.Username.Should().Be(TestDatabaseSeeder.TestUserUsername);
		result.Email.Should().Be(TestDatabaseSeeder.TestUserEmail);
	}

	[Fact]
	public async Task GetByUsername_WhenBlockedRelationshipExists_Returns200WithMetricsZeroed()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var currentUserId = await GetUserIdAsync(TestDatabaseSeeder.TestUserEmail);
		var targetUserId = await GetUserIdAsync(TestDatabaseSeeder.AdminEmail);
		await EnsureBlockRelationAsync(currentUserId, targetUserId);

		var response = await authClient.GetAsync("/api/v1/users/admin");
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<UserProfileResponse>(body, _json);

		result.Should().NotBeNull();
		result!.Username.Should().Be("admin");
		result.IsBlocked.Should().BeTrue();
		result.IsBlockedByMe.Should().BeTrue();
		result.FollowersCount.Should().Be(0);
		result.FollowingCount.Should().Be(0);
		result.TripCount.Should().Be(0);
		result.PostCount.Should().Be(0);
	}

	[Fact]
	public async Task GetMe_WithValidToken_Returns200()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.GetAsync("/api/v1/users/me");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<UserProfileResponse>(body, _json);

		result.Should().NotBeNull();
		result!.Username.Should().Be(TestDatabaseSeeder.TestUserUsername);
		result.IsFollowing.Should().BeFalse();
	}

	[Fact]
	public async Task UpdateMe_WithoutToken_Returns401()
	{
		var response = await _client.PutAsJsonAsync("/api/v1/users/me", new UpdateProfileRequest
		{
			Bio = "Updated bio",
			ProfilePhotoUrl = "https://cdn.example.com/profile.jpg"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateMe_WithValidToken_Returns204AndPersistsChanges()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.PutAsJsonAsync("/api/v1/users/me", new UpdateProfileRequest
		{
			Bio = "Updated travel bio",
			ProfilePhotoUrl = "https://cdn.example.com/new-profile.jpg"
		});

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var user = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail);

		user.Bio.Should().Be("Updated travel bio");
		user.ProfilePhotoUrl.Should().Be("https://cdn.example.com/new-profile.jpg");
	}

	private async Task<User> CreateUserAsync(
		string username,
		int karmaScore,
		bool isSuspended,
		string? profilePhotoUrl = null)
	{
		var user = new User
		{
			Id = Guid.NewGuid(),
			Username = username,
			Email = $"{username}@omniflow.com",
			KarmaScore = karmaScore,
			Role = Roles.Traveler,
			IsSuspended = isSuspended,
			ProfilePhotoUrl = profilePhotoUrl
		};

		using var scope = _factory.Services.CreateScope();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var appUser = new ApplicationUser
		{
			Id = user.Id,
			UserName = user.Username,
			Email = user.Email,
			EmailConfirmed = true
		};

		var result = await userManager.CreateAsync(appUser, "TestUser123!");
		result.Succeeded.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.Description)));

		db.Users.Add(user);
		await db.SaveChangesAsync();

		return user;
	}
}
