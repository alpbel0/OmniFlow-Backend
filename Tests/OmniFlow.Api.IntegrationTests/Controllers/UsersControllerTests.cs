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

	[Fact]
	public async Task GetTopContributors_WithoutToken_Returns200AndExcludesSuspendedUsers()
	{
		var suffix = Guid.NewGuid().ToString("N")[..8];
		var activeUser = await CreateUserAsync($"leader_{suffix}", 900, isSuspended: false, "https://cdn.example.com/leader.jpg");
		var suspendedUser = await CreateUserAsync($"suspended_{suffix}", 1000, isSuspended: true);

		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			db.Trips.Add(new Trip
			{
				Id = Guid.NewGuid(),
				OwnerId = activeUser.Id,
				Title = $"Leaderboard Trip {suffix}",
				Status = TripStatus.Published,
				City = "Antalya",
				Country = "Turkey",
				StartDate = new DateOnly(2026, 6, 1),
				EndDate = new DateOnly(2026, 6, 5),
				PersonCount = 2,
				BudgetTier = BudgetTier.Standard,
				TravelStyle = TravelStyle.Adventure
			});
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
