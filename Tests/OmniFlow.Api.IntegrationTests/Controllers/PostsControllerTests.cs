using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class PostsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public PostsControllerTests(CustomWebApplicationFactory factory)
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

	private static CreatePostRequest CreateValidPostRequest() => new()
	{
		PostType = PostType.Photo,
		Content = "Sunset in Antalya",
		Photos = new List<string> { "https://cdn.example.com/posts/sunset.jpg" },
		Tags = new List<string> { "travel", "sunset" },
		AiTags = new List<string> { "coast", "golden-hour" },
		City = "Antalya",
		Country = "Turkey"
	};

	private async Task<Guid> CreatePostAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/posts", CreateValidPostRequest());
		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<Guid>(body);
	}

	private async Task<Guid> GetUserIdAsync(string email)
	{
		using var scope = _factory.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		return dbContext.Users.Single(x => x.Email == email).Id;
	}

	private async Task EnsureBlockRelationAsync(Guid blockerId, Guid blockedUserId)
	{
		using var scope = _factory.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

		if (!dbContext.Blocks.Any(x => x.BlockerId == blockerId && x.BlockedUserId == blockedUserId))
		{
			dbContext.Blocks.Add(new Block { BlockerId = blockerId, BlockedUserId = blockedUserId });
			await dbContext.SaveChangesAsync();
		}
	}

	[Fact]
	public async Task GetById_WithoutToken_Returns401()
	{
		var response = await _client.GetAsync($"/api/v1/posts/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetLikedPosts_WithoutToken_Returns401()
	{
		var response = await _client.GetAsync("/api/v1/posts/liked");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetLikedPosts_WithValidToken_ReturnsLikedPosts()
	{
		var likerToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var likerClient = CreateAuthenticatedClient(likerToken);
		var postId = await CreatePostAsync(likerClient);

		var upvoteResponse = await likerClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var response = await likerClient.GetAsync("/api/v1/posts/liked?pageNumber=1&pageSize=10");
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PagedResponse<PostResponse>>(body, _json);

		result.Should().NotBeNull();
		result!.Data.Should().ContainSingle(post => post.Id == postId);
		result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
		result.Data[0].IsUpvoted.Should().BeTrue();
	}

	[Fact]
	public async Task GetById_WithNonExistentId_Returns404()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.GetAsync($"/api/v1/posts/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetById_WithValidToken_Returns200()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		var response = await authClient.GetAsync($"/api/v1/posts/{postId}");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PostResponse>(body, _json);

		result.Should().NotBeNull();
		result!.Id.Should().Be(postId);
		result.Content.Should().Be("Sunset in Antalya");
		result.Username.Should().Be(TestDatabaseSeeder.TestUserUsername);
	}

	[Fact]
	public async Task GetById_WhenPostOwnerIsBlocked_Returns404()
	{
		var testUserToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);

		var testUserClient = CreateAuthenticatedClient(testUserToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var testUserId = await GetUserIdAsync(TestDatabaseSeeder.TestUserEmail);
		var adminId = await GetUserIdAsync(TestDatabaseSeeder.AdminEmail);

		var postId = await CreatePostAsync(adminClient);
		await EnsureBlockRelationAsync(testUserId, adminId);

		var response = await testUserClient.GetAsync($"/api/v1/posts/{postId}");

		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Create_WithoutToken_Returns401()
	{
		var response = await _client.PostAsJsonAsync("/api/v1/posts", CreateValidPostRequest());

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Create_WithValidToken_Returns201()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var response = await authClient.PostAsJsonAsync("/api/v1/posts", CreateValidPostRequest());

		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		var postId = JsonSerializer.Deserialize<Guid>(body);

		postId.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public async Task Update_WithoutToken_Returns401()
	{
		var response = await _client.PutAsJsonAsync($"/api/v1/posts/{Guid.NewGuid()}", new UpdatePostRequest
		{
			Content = "Updated content",
			Tags = new List<string> { "updated" }
		});

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Update_WithValidToken_Returns204AndPersistsChanges()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		var updateResponse = await authClient.PutAsJsonAsync($"/api/v1/posts/{postId}", new UpdatePostRequest
		{
			Content = "Updated sunset content",
			Tags = new List<string> { "updated", "travel" }
		});

		updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await authClient.GetAsync($"/api/v1/posts/{postId}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await getResponse.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PostResponse>(body, _json);

		result!.Content.Should().Be("Updated sunset content");
		result.Tags.Should().Contain(new[] { "updated", "travel" });
	}

	[Fact]
	public async Task Delete_WithoutToken_Returns401()
	{
		var response = await _client.DeleteAsync($"/api/v1/posts/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Delete_WithValidToken_Returns204AndRemovesPost()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		var deleteResponse = await authClient.DeleteAsync($"/api/v1/posts/{postId}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await authClient.GetAsync($"/api/v1/posts/{postId}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Upvote_WithoutToken_Returns401()
	{
		var response = await _client.PostAsync($"/api/v1/posts/{Guid.NewGuid()}/upvote", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Upvote_WithValidToken_Returns204AndIncrementsCount()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		var upvoteResponse = await authClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await authClient.GetAsync($"/api/v1/posts/{postId}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await getResponse.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PostResponse>(body, _json);

		result!.UpvoteCount.Should().Be(1);
		result.IsUpvoted.Should().BeTrue();
	}

	[Fact]
	public async Task RemoveUpvote_WithoutToken_Returns401()
	{
		var response = await _client.DeleteAsync($"/api/v1/posts/{Guid.NewGuid()}/upvote");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RemoveUpvote_WithValidToken_Returns204AndDecrementsCount()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		// First upvote
		await authClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);

		// Then remove upvote
		var removeResponse = await authClient.DeleteAsync($"/api/v1/posts/{postId}/upvote");
		removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await authClient.GetAsync($"/api/v1/posts/{postId}");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await getResponse.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<PostResponse>(body, _json);

		result!.UpvoteCount.Should().Be(0);
		result.IsUpvoted.Should().BeFalse();
	}

	[Fact]
	public async Task RemoveUpvote_WithoutExistingUpvote_Returns404()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);

		var postId = await CreatePostAsync(authClient);

		var removeResponse = await authClient.DeleteAsync($"/api/v1/posts/{postId}/upvote");
		removeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
