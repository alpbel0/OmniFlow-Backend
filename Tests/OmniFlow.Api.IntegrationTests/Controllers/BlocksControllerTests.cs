using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Blocks;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class BlocksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public BlocksControllerTests(CustomWebApplicationFactory factory)
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

	private void ResetBlockAndFollowState(Guid currentUserId, Guid targetUserId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();

		var existingBlock = db.Blocks.FirstOrDefault(x => x.BlockerId == currentUserId && x.BlockedUserId == targetUserId);
		if (existingBlock != null)
		{
			db.Blocks.Remove(existingBlock);
		}

		var outboundFollow = db.Follows.FirstOrDefault(x => x.FollowerId == currentUserId && x.FollowingId == targetUserId);
		if (outboundFollow != null)
		{
			db.Follows.Remove(outboundFollow);
		}

		var inboundFollow = db.Follows.FirstOrDefault(x => x.FollowerId == targetUserId && x.FollowingId == currentUserId);
		if (inboundFollow != null)
		{
			db.Follows.Remove(inboundFollow);
		}

		var currentUser = db.Users.Single(x => x.Id == currentUserId);
		var targetUser = db.Users.Single(x => x.Id == targetUserId);
		currentUser.FollowersCount = 0;
		currentUser.FollowingCount = 0;
		targetUser.FollowersCount = 0;
		targetUser.FollowingCount = 0;

		db.SaveChangesAsync().GetAwaiter().GetResult();
	}

	[Fact]
	public async Task Block_WithoutToken_Returns401()
	{
		var response = await _client.PostAsync($"/api/v1/users/{Guid.NewGuid()}/block", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Block_SelfBlock_Returns409()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;

		var response = await authClient.PostAsync($"/api/v1/users/{currentUserId}/block", null);

		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task Block_TargetUserNotFound_Returns404()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.PostAsync($"/api/v1/users/{Guid.NewGuid()}/block", null);

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Block_ThenUnblock_Returns204AndUpdatesBlockedUsers()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		ResetBlockAndFollowState(currentUserId, targetUserId);

		var blockResponse = await authClient.PostAsync($"/api/v1/users/{targetUserId}/block", null);
		blockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var listAfterBlock = await authClient.GetAsync($"/api/v1/users/{currentUserId}/blocked-users?pageNumber=1&pageSize=10");
		listAfterBlock.StatusCode.Should().Be(HttpStatusCode.OK);

		var listBody = await listAfterBlock.Content.ReadAsStringAsync();
		var blockedUsers = JsonSerializer.Deserialize<PagedResponse<BlockedUserResponse>>(listBody, _json);
		blockedUsers.Should().NotBeNull();
		blockedUsers!.Data.Should().ContainSingle(user => user.Id == targetUserId);

		var unblockResponse = await authClient.DeleteAsync($"/api/v1/users/{targetUserId}/block");
		unblockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var listAfterUnblock = await authClient.GetAsync($"/api/v1/users/{currentUserId}/blocked-users?pageNumber=1&pageSize=10");
		listAfterUnblock.StatusCode.Should().Be(HttpStatusCode.OK);

		var listAfterUnblockBody = await listAfterUnblock.Content.ReadAsStringAsync();
		var blockedUsersAfterUnblock = JsonSerializer.Deserialize<PagedResponse<BlockedUserResponse>>(listAfterUnblockBody, _json);
		blockedUsersAfterUnblock.Should().NotBeNull();
		blockedUsersAfterUnblock!.Data.Should().NotContain(user => user.Id == targetUserId);
	}

	[Fact]
	public async Task Block_WhenAlreadyBlocked_ShouldRemainSingleBlockRow()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		ResetBlockAndFollowState(currentUserId, targetUserId);

		var first = await authClient.PostAsync($"/api/v1/users/{targetUserId}/block", null);
		var second = await authClient.PostAsync($"/api/v1/users/{targetUserId}/block", null);

		first.StatusCode.Should().Be(HttpStatusCode.NoContent);
		second.StatusCode.Should().Be(HttpStatusCode.NoContent);

		using var validationScope = _factory.Services.CreateScope();
		var validationDb = validationScope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		validationDb.Blocks.Count(x => x.BlockerId == currentUserId && x.BlockedUserId == targetUserId).Should().Be(1);
	}

	[Fact]
	public async Task Unblock_WhenNotBlocked_ShouldReturn204()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		ResetBlockAndFollowState(currentUserId, targetUserId);

		var response = await authClient.DeleteAsync($"/api/v1/users/{targetUserId}/block");

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task GetBlockedUsers_ForAnotherUser_Returns403()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.GetAsync($"/api/v1/users/{Guid.NewGuid()}/blocked-users?pageNumber=1&pageSize=10");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task Block_RemovesMutualFollowRelations()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		var currentUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var targetUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		ResetBlockAndFollowState(currentUserId, targetUserId);

		db.Follows.Add(new Follow { FollowerId = currentUserId, FollowingId = targetUserId });
		db.Follows.Add(new Follow { FollowerId = targetUserId, FollowingId = currentUserId });

		var currentUser = db.Users.Single(x => x.Id == currentUserId);
		var targetUser = db.Users.Single(x => x.Id == targetUserId);
		currentUser.FollowingCount = 1;
		currentUser.FollowersCount = 1;
		targetUser.FollowingCount = 1;
		targetUser.FollowersCount = 1;

		await db.SaveChangesAsync();

		var blockResponse = await authClient.PostAsync($"/api/v1/users/{targetUserId}/block", null);
		blockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		using var validationScope = _factory.Services.CreateScope();
		var validationDb = validationScope.ServiceProvider.GetRequiredService<OmniFlow.Application.Interfaces.IApplicationDbContext>();
		validationDb.Follows.Any(x => x.FollowerId == currentUserId && x.FollowingId == targetUserId).Should().BeFalse();
		validationDb.Follows.Any(x => x.FollowerId == targetUserId && x.FollowingId == currentUserId).Should().BeFalse();

		var validatedCurrentUser = validationDb.Users.Single(x => x.Id == currentUserId);
		var validatedTargetUser = validationDb.Users.Single(x => x.Id == targetUserId);
		validatedCurrentUser.FollowingCount.Should().Be(0);
		validatedCurrentUser.FollowersCount.Should().Be(0);
		validatedTargetUser.FollowingCount.Should().Be(0);
		validatedTargetUser.FollowersCount.Should().Be(0);
	}
}
