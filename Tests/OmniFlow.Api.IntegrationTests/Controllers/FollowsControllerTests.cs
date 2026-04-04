using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class FollowsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public FollowsControllerTests(CustomWebApplicationFactory factory)
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

	private void ResetTestFollowState(Guid targetUserId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUser = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail);
		var targetUser = db.Users.Single(x => x.Id == targetUserId);
		var existingFollow = db.Follows.FirstOrDefault(x => x.FollowerId == currentUser.Id && x.FollowingId == targetUserId);

		if (existingFollow != null)
		{
			db.Follows.Remove(existingFollow);
		}

		currentUser.FollowingCount = 0;
		targetUser.FollowersCount = 0;
		db.SaveChangesAsync().GetAwaiter().GetResult();
	}

	[Fact]
	public async Task Follow_WithoutToken_Returns401()
	{
		var response = await _client.PostAsync($"/api/v1/users/{Guid.NewGuid()}/follow", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Follow_SelfFollow_Returns409()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;

		var response = await authClient.PostAsync($"/api/v1/users/{currentUserId}/follow", null);

		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task Follow_ThenUnfollow_Returns204AndUpdatesCounters()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		ResetTestFollowState(targetUserId);

		var followResponse = await authClient.PostAsync($"/api/v1/users/{targetUserId}/follow", null);
		followResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		using (var validationScope = _factory.Services.CreateScope())
		{
			var validationDb = validationScope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
			var currentUser = validationDb.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail);
			var targetUser = validationDb.Users.Single(x => x.Id == targetUserId);

			currentUser.FollowingCount.Should().Be(1);
			targetUser.FollowersCount.Should().Be(1);
		}

		var unfollowResponse = await authClient.DeleteAsync($"/api/v1/users/{targetUserId}/follow");
		unfollowResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		using (var validationScope = _factory.Services.CreateScope())
		{
			var validationDb = validationScope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
			var currentUser = validationDb.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail);
			var targetUser = validationDb.Users.Single(x => x.Id == targetUserId);

			currentUser.FollowingCount.Should().Be(0);
			targetUser.FollowersCount.Should().Be(0);
		}
	}

	[Fact]
	public async Task Followers_AndFollowing_Endpoints_Return200()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		ResetTestFollowState(targetUserId);
		await authClient.PostAsync($"/api/v1/users/{targetUserId}/follow", null);

		var followersResponse = await authClient.GetAsync($"/api/v1/users/{targetUserId}/followers?pageNumber=1&pageSize=10");
		followersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var followersBody = await followersResponse.Content.ReadAsStringAsync();
		var followers = JsonSerializer.Deserialize<PagedResponse<FollowUserResponse>>(followersBody, _json);
		followers.Should().NotBeNull();
		followers!.Data.Should().ContainSingle(user => user.Id == currentUserId && user.IsFollowing == false);

		var followingResponse = await authClient.GetAsync($"/api/v1/users/{currentUserId}/following?pageNumber=1&pageSize=10");
		followingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var followingBody = await followingResponse.Content.ReadAsStringAsync();
		var following = JsonSerializer.Deserialize<PagedResponse<FollowUserResponse>>(followingBody, _json);
		following.Should().NotBeNull();
		following!.Data.Should().ContainSingle(user => user.Id == targetUserId && user.IsFollowing == true);
	}
}
