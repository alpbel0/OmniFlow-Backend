using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.DTOs.Stops;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class KarmaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public KarmaIntegrationTests(CustomWebApplicationFactory factory)
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

	private (Guid TestUserId, Guid AdminUserId) GetUserIds()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var testUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;
		var adminUserId = db.Users.Single(x => x.Email == TestDatabaseSeeder.AdminEmail).Id;
		return (testUserId, adminUserId);
	}

	private async Task<Guid> CreateTripAsync(HttpClient authClient, string title)
	{
		var createResponse = await authClient.PostAsJsonAsync("/api/v1/trips", new CreateTripRequest
		{
			Title = title,
			City = "Antalya",
			Country = "Turkey",
			StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
			EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(2)),
			PersonCount = 2,
			BudgetTier = BudgetTier.Standard,
			TravelStyle = TravelStyle.Adventure
		});
		createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		return JsonSerializer.Deserialize<Guid>(await createResponse.Content.ReadAsStringAsync());
	}

	private async Task AddStopAndPublishAsync(HttpClient authClient, Guid tripId)
	{
		var stopResponse = await authClient.PostAsJsonAsync($"/api/v1/trips/{tripId}/stops", new CreateStopRequest
		{
			DayNumber = 1,
			CustomName = "Publish Stop",
			CustomCategory = PlaceCategory.Restaurant,
			DurationMinutes = 60
		});
		stopResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		var publishResponse = await authClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
		publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
	}

	private async Task<Guid> CreatePostAsync(HttpClient authClient, string content)
	{
		var response = await authClient.PostAsJsonAsync("/api/v1/posts", new CreatePostRequest
		{
			PostType = PostType.Photo,
			Content = content,
			Photos = new List<string> { "https://cdn.example.com/posts/karma.jpg" },
			City = "Antalya",
			Country = "Turkey"
		});
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		return JsonSerializer.Deserialize<Guid>(await response.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Karma_PublishTrip_CreatesEventAndUpdatesUserScore()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var (testUserId, _) = GetUserIds();
		var tripId = await CreateTripAsync(testClient, $"karma-publish-{Guid.NewGuid():N}");
		int beforeScore;
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			beforeScore = db.Users.Single(x => x.Id == testUserId).KarmaScore;
		}
		await AddStopAndPublishAsync(testClient, tripId);
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			var afterScore = db.Users.Single(x => x.Id == testUserId).KarmaScore;
			afterScore.Should().Be(beforeScore + 10);
			db.KarmaEvents.Count(x =>
				x.UserId == testUserId &&
				x.EventType == KarmaEventType.TripPublished &&
				x.SourceId == tripId).Should().Be(1);
		}
	}

	[Fact]
	public async Task Karma_ForkTrip_GrantsOriginalOwnerPlusFive()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);
		var (testUserId, adminUserId) = GetUserIds();
		var tripId = await CreateTripAsync(adminClient, $"karma-fork-{Guid.NewGuid():N}");
		await AddStopAndPublishAsync(adminClient, tripId);
		int beforeAdminScore;
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			beforeAdminScore = db.Users.Single(x => x.Id == adminUserId).KarmaScore;
		}
		var forkResponse = await testClient.PostAsync($"/api/v1/trips/{tripId}/fork", null);
		forkResponse.StatusCode.Should().Be(HttpStatusCode.Created);
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			var afterAdminScore = db.Users.Single(x => x.Id == adminUserId).KarmaScore;
			afterAdminScore.Should().Be(beforeAdminScore + 5);
			db.KarmaEvents.Count(x =>
				x.UserId == adminUserId &&
				x.ActorId == testUserId &&
				x.EventType == KarmaEventType.TripForked &&
				x.SourceId == tripId).Should().Be(1);
		}
	}

	[Fact]
	public async Task Karma_Farming_DuplicatePublishAttempt_DoesNotCreateSecondEvent()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var (testUserId, _) = GetUserIds();
		var tripId = await CreateTripAsync(testClient, $"karma-farming-{Guid.NewGuid():N}");
		await AddStopAndPublishAsync(testClient, tripId);
		var secondPublish = await testClient.PostAsync($"/api/v1/trips/{tripId}/publish", null);
		secondPublish.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		db.KarmaEvents.Count(x =>
			x.UserId == testUserId &&
			x.EventType == KarmaEventType.TripPublished &&
			x.SourceId == tripId).Should().Be(1);
	}

	[Fact]
	public async Task Karma_DuplicateUpvote_CreatesSingleKarmaEvent()
	{
		var testToken = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var adminToken = await GetAccessTokenAsync(TestDatabaseSeeder.AdminEmail, TestDatabaseSeeder.AdminPassword);
		var testClient = CreateAuthenticatedClient(testToken);
		var adminClient = CreateAuthenticatedClient(adminToken);
		var (testUserId, adminUserId) = GetUserIds();
		var postId = await CreatePostAsync(adminClient, $"karma-upvote-{Guid.NewGuid():N}");
		int beforeAdminScore;
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			beforeAdminScore = db.Users.Single(x => x.Id == adminUserId).KarmaScore;
		}
		var firstUpvote = await testClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		firstUpvote.StatusCode.Should().Be(HttpStatusCode.NoContent);
		var duplicateUpvote = await testClient.PostAsync($"/api/v1/posts/{postId}/upvote", null);
		duplicateUpvote.StatusCode.Should().Be(HttpStatusCode.Conflict);
		using (var scope = _factory.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
			var afterAdminScore = db.Users.Single(x => x.Id == adminUserId).KarmaScore;
			afterAdminScore.Should().Be(beforeAdminScore + 1);
			db.KarmaEvents.Count(x =>
				x.UserId == adminUserId &&
				x.ActorId == testUserId &&
				x.EventType == KarmaEventType.PostUpvoted &&
				x.SourceId == postId).Should().Be(1);
		}
	}
}
