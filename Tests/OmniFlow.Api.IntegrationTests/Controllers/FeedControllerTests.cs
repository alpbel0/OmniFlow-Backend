using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Features.Posts.Queries.GetFeed;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class FeedControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FeedControllerTests(CustomWebApplicationFactory factory)
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

    private static CreatePostRequest CreateValidPostRequest(string content) => new()
    {
        PostType = PostType.Photo,
        Content = content,
        Photos = new List<string> { "https://cdn.example.com/posts/feed.jpg" },
        Tags = new List<string> { "travel" },
        City = "Antalya",
        Country = "Turkey"
    };

    private async Task<Guid> CreatePostAsync(HttpClient client, string content)
    {
        var response = await client.PostAsJsonAsync("/api/v1/posts", CreateValidPostRequest(content));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Guid>(body);
    }

    private async Task SetPostCreatedAtAsync(Guid postId, DateTime createdAt)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var post = dbContext.Posts.Single(x => x.Id == postId);
        post.CreatedAt = createdAt;
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureFollowRelationAsync(Guid followerId, Guid followingId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (!dbContext.Follows.Any(x => x.FollowerId == followerId && x.FollowingId == followingId))
        {
            dbContext.Follows.Add(new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId
            });

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        return dbContext.Users.Single(x => x.Email == email).Id;
    }

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/feed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_LatestTab_ReturnsLatestPost()
    {
        var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var authClient = CreateAuthenticatedClient(token);

        var olderPostId = await CreatePostAsync(authClient, "Older feed post");
        var newerPostId = await CreatePostAsync(authClient, "Newer feed post");

        await SetPostCreatedAtAsync(olderPostId, DateTime.UtcNow.AddMinutes(-2));
        await SetPostCreatedAtAsync(newerPostId, DateTime.UtcNow.AddMinutes(-1));

        var response = await authClient.GetAsync("/api/v1/feed?tab=Latest&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetFeedViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(1);
        result.Data[0].Content.Should().Be("Newer feed post");
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_FollowingTab_ReturnsOnlyFollowedUsersPosts()
    {
        var testUserToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
        var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);

        var testUserClient = CreateAuthenticatedClient(testUserToken);
        var adminClient = CreateAuthenticatedClient(adminToken);

        var testUserId = await GetUserIdAsync(TestDatabaseSeeder.TestUserEmail);
        var adminId = await GetUserIdAsync(TestDatabaseSeeder.AdminEmail);

        await EnsureFollowRelationAsync(testUserId, adminId);

        await CreatePostAsync(testUserClient, "Test user post");
        var adminPostId = await CreatePostAsync(adminClient, "Admin feed post");

        await SetPostCreatedAtAsync(adminPostId, DateTime.UtcNow.AddMinutes(-1));

        var response = await testUserClient.GetAsync("/api/v1/feed?tab=Following&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetFeedViewModel>(body, _json);

        result.Should().NotBeNull();
        result!.Data.Should().ContainSingle();
        result.Data[0].UserId.Should().Be(adminId);
        result.Data[0].Content.Should().Be("Admin feed post");
    }
}