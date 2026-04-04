using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class NotificationsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public NotificationsControllerTests(CustomWebApplicationFactory factory)
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

	private async Task<int> GetUnreadCountAsync(HttpClient authClient)
	{
		var response = await authClient.GetAsync("/api/v1/notifications/unread-count");
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<int>(body);
	}

	private async Task<PagedResponse<NotificationResponse>> GetNotificationsAsync(HttpClient authClient, bool? isRead = null)
	{
		var url = "/api/v1/notifications?pageNumber=1&pageSize=100";
		if (isRead.HasValue)
		{
			url += $"&isRead={isRead.Value.ToString().ToLowerInvariant()}";
		}

		var response = await authClient.GetAsync(url);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<PagedResponse<NotificationResponse>>(body, _json)!;
	}

	private async Task<Guid> CreatePostAsync(HttpClient authClient, string content)
	{
		var response = await authClient.PostAsJsonAsync("/api/v1/posts", new CreatePostRequest
		{
			PostType = PostType.Photo,
			Content = content,
			Photos = new List<string> { "https://cdn.example.com/posts/notification.jpg" },
			City = "Antalya",
			Country = "Turkey"
		});

		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<Guid>(body);
	}

	private void EnsureNotFollowing(Guid followerId, Guid followingId)
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var follow = db.Follows.FirstOrDefault(x => x.FollowerId == followerId && x.FollowingId == followingId);
		if (follow != null)
		{
			db.Follows.Remove(follow);
			db.SaveChangesAsync().GetAwaiter().GetResult();
		}
	}

	private (Guid TestUserId, Guid AdminUserId) GetUserIds()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var testUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var adminUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		return (testUserId, adminUserId);
	}

	[Fact]
	public async Task Notifications_Follow_CreatesFollowNotification()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);
		var (testUserId, adminUserId) = GetUserIds();

		EnsureNotFollowing(testUserId, adminUserId);
		var baselineUnread = await GetUnreadCountAsync(adminClient);

		var followResponse = await testClient.PostAsync($"/api/v1/users/{adminUserId}/follow", null);
		followResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var unreadAfter = await GetUnreadCountAsync(adminClient);
		unreadAfter.Should().Be(baselineUnread + 1);

		var notifications = await GetNotificationsAsync(adminClient, false);
		notifications.Data.Should().Contain(x => x.Type == NotificationType.Follow && x.IsRead == false);
	}

	[Fact]
	public async Task Notifications_Upvote_CreatesPostUpvoteNotification()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var postId = await CreatePostAsync(adminClient, $"admin-post-{Guid.NewGuid():N}");
		var baselineUnread = await GetUnreadCountAsync(adminClient);

		var upvoteResponse = await testClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var unreadAfter = await GetUnreadCountAsync(adminClient);
		unreadAfter.Should().Be(baselineUnread + 1);

		var notifications = await GetNotificationsAsync(adminClient, false);
		notifications.Data.Should().Contain(x => x.Type == NotificationType.PostUpvote && x.TargetId == postId);
	}

	[Fact]
	public async Task Notifications_SelfUpvote_DoesNotCreateNotification()
	{
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var postId = await CreatePostAsync(adminClient, $"self-upvote-post-{Guid.NewGuid():N}");
		var baselineUnread = await GetUnreadCountAsync(adminClient);

		var upvoteResponse = await adminClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var unreadAfter = await GetUnreadCountAsync(adminClient);
		unreadAfter.Should().Be(baselineUnread);
	}

	[Fact]
	public async Task Notifications_Comment_CreatesCommentNotification()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var postId = await CreatePostAsync(adminClient, $"comment-post-{Guid.NewGuid():N}");
		var baselineUnread = await GetUnreadCountAsync(adminClient);

		var commentResponse = await testClient.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new CreateCommentRequest
		{
			Content = "Great route",
			Mentions = new List<string>()
		});
		commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

		var unreadAfter = await GetUnreadCountAsync(adminClient);
		unreadAfter.Should().Be(baselineUnread + 1);

		var notifications = await GetNotificationsAsync(adminClient, false);
		notifications.Data.Should().Contain(x => x.Type == NotificationType.Comment && x.TargetId == postId);
	}

	[Fact]
	public async Task Notifications_Mention_CreatesMentionNotification()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var postId = await CreatePostAsync(adminClient, $"mention-post-{Guid.NewGuid():N}");
		var baselineUnread = await GetUnreadCountAsync(testClient);

		var commentResponse = await adminClient.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new CreateCommentRequest
		{
			Content = "@testuser check this",
			Mentions = new List<string> { "@testuser" }
		});
		commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

		var unreadAfter = await GetUnreadCountAsync(testClient);
		unreadAfter.Should().Be(baselineUnread + 1);

		var notifications = await GetNotificationsAsync(testClient, false);
		notifications.Data.Should().Contain(x => x.Type == NotificationType.Mention && x.TargetId == postId);
	}

	[Fact]
	public async Task Notifications_MarkRead_UpdatesIsReadAndReadAt()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var postId = await CreatePostAsync(adminClient, $"mark-read-post-{Guid.NewGuid():N}");

		var commentResponse = await testClient.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new CreateCommentRequest
		{
			Content = "mark read trigger"
		});
		commentResponse.StatusCode.Should().Be(HttpStatusCode.Created);

		var unreadNotifications = await GetNotificationsAsync(adminClient, false);
		var targetNotification = unreadNotifications.Data
			.FirstOrDefault(x => x.Type == NotificationType.Comment && x.TargetId == postId);
		targetNotification.Should().NotBeNull();

		var markReadResponse = await adminClient.PostAsync($"/api/v1/notifications/{targetNotification!.Id}/read", null);
		markReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var allNotifications = await GetNotificationsAsync(adminClient);
		var updated = allNotifications.Data.Single(x => x.Id == targetNotification.Id);
		updated.IsRead.Should().BeTrue();
		updated.ReadAt.Should().NotBeNull();
	}

	[Fact]
	public async Task Notifications_UnreadCount_DecreasesAfterMarkAsRead()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);

		var baselineUnread = await GetUnreadCountAsync(adminClient);
		var post1 = await CreatePostAsync(adminClient, $"unread-1-{Guid.NewGuid():N}");
		var post2 = await CreatePostAsync(adminClient, $"unread-2-{Guid.NewGuid():N}");
		var post3 = await CreatePostAsync(adminClient, $"unread-3-{Guid.NewGuid():N}");

		(await testClient.PostAsJsonAsync($"/api/v1/posts/{post1}/comments", new CreateCommentRequest { Content = "n1" })).StatusCode.Should().Be(HttpStatusCode.Created);
		(await testClient.PostAsJsonAsync($"/api/v1/posts/{post2}/comments", new CreateCommentRequest { Content = "n2" })).StatusCode.Should().Be(HttpStatusCode.Created);
		(await testClient.PostAsJsonAsync($"/api/v1/posts/{post3}/comments", new CreateCommentRequest { Content = "n3" })).StatusCode.Should().Be(HttpStatusCode.Created);

		var unreadAfterCreate = await GetUnreadCountAsync(adminClient);
		unreadAfterCreate.Should().Be(baselineUnread + 3);

		var unreadNotifications = await GetNotificationsAsync(adminClient, false);
		var notificationToRead = unreadNotifications.Data.First();

		var markReadResponse = await adminClient.PostAsync($"/api/v1/notifications/{notificationToRead.Id}/read", null);
		markReadResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var unreadAfterRead = await GetUnreadCountAsync(adminClient);
		unreadAfterRead.Should().Be(unreadAfterCreate - 1);
	}
}
