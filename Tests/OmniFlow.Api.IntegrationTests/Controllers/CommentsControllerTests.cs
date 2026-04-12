using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class CommentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public CommentsControllerTests(CustomWebApplicationFactory factory)
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
		Content = "Comment test post",
		Photos = new List<string> { "https://cdn.example.com/posts/comment-test.jpg" },
		City = "Antalya",
		Country = "Turkey"
	};

	private static CreateCommentRequest CreateValidCommentRequest(string content = "Great trip") => new()
	{
		Content = content,
		Mentions = new List<string>()
	};

	private async Task<Guid> CreatePostAsync(HttpClient client)
	{
		var response = await client.PostAsJsonAsync("/api/v1/posts", CreateValidPostRequest());
		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<Guid>(body);
	}

	private async Task<Guid> CreateCommentAsync(HttpClient client, Guid postId, string content = "Great trip")
	{
		var response = await client.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", CreateValidCommentRequest(content));
		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<Guid>(body);
	}

	[Fact]
	public async Task GetByPost_WithoutToken_Returns401()
	{
		var response = await _client.GetAsync($"/api/v1/posts/{Guid.NewGuid()}/comments");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task GetByPost_WithValidToken_Returns200()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);
		await CreateCommentAsync(authClient, postId);

		var response = await authClient.GetAsync($"/api/v1/posts/{postId}/comments");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<CommentResponse>>(body, _json);

		result.Should().NotBeNull();
		result!.Data.Should().HaveCount(1);
		result.Data[0].Content.Should().Be("Great trip");
	}

	[Fact]
	public async Task Create_WithoutToken_Returns401()
	{
		var response = await _client.PostAsJsonAsync($"/api/v1/posts/{Guid.NewGuid()}/comments", CreateValidCommentRequest());

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Create_WithValidToken_Returns201()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);

		var response = await authClient.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", CreateValidCommentRequest());

		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		var commentId = JsonSerializer.Deserialize<Guid>(body);

		commentId.Should().NotBe(Guid.Empty);
	}

	[Fact]
	public async Task Delete_WithoutToken_Returns401()
	{
		var response = await _client.DeleteAsync($"/api/v1/comments/{Guid.NewGuid()}");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Delete_WithValidToken_Returns204()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);
		var commentId = await CreateCommentAsync(authClient, postId);

		var deleteResponse = await authClient.DeleteAsync($"/api/v1/comments/{commentId}");

		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var getResponse = await authClient.GetAsync($"/api/v1/posts/{postId}/comments");
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await getResponse.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<CommentResponse>>(body, _json);

		result!.Data.Should().BeEmpty();
	}

	[Fact]
	public async Task Upvote_WithoutToken_Returns401()
	{
		var response = await _client.PostAsync($"/api/v1/comments/{Guid.NewGuid()}/upvote", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Upvote_WithValidToken_Returns204()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);
		var commentId = await CreateCommentAsync(authClient, postId);

		var upvoteResponse = await authClient.PostAsync($"/api/v1/comments/{commentId}/upvote", null);

		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task RemoveUpvote_WithoutToken_Returns401()
	{
		var response = await _client.DeleteAsync($"/api/v1/comments/{Guid.NewGuid()}/upvote");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RemoveUpvote_WithValidToken_Returns204()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);
		var commentId = await CreateCommentAsync(authClient, postId);

		// First upvote
		await authClient.PostAsync($"/api/v1/comments/{commentId}/upvote", null);

		// Then remove upvote
		var removeResponse = await authClient.DeleteAsync($"/api/v1/comments/{commentId}/upvote");

		removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	[Fact]
	public async Task RemoveUpvote_WithoutExistingUpvote_Returns404()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var postId = await CreatePostAsync(authClient);
		var commentId = await CreateCommentAsync(authClient, postId);

		var removeResponse = await authClient.DeleteAsync($"/api/v1/comments/{commentId}/upvote");

		removeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}