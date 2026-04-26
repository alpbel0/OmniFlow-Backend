using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class CommunityTipsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	private static readonly JsonSerializerOptions _json = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public CommunityTipsControllerTests(CustomWebApplicationFactory factory)
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

	private async Task<(Guid TripId, Guid PlaceId)> SeedTripAndPlaceAsync()
	{
		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var ownerId = db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id;

		var trip = new Trip
		{
			Id = Guid.NewGuid(),
			OwnerId = ownerId,
			Title = "Weekend in Antalya",
			Origin = "Antalya",
			OriginCountry = "Turkey",
			BudgetTier = BudgetTier.Standard,
			TravelStyles = new List<TravelStyle> { TravelStyle.Relax },
			PersonCount = 2,
			Status = TripStatus.Published
		};

		var place = new Place
		{
			Id = Guid.NewGuid(),
			Name = "Coastal Bakery",
			Category = PlaceCategory.Restaurant,
			Latitude = 36.9,
			Longitude = 30.7,
			City = "Antalya",
			Country = "Turkey"
		};

		db.Trips.Add(trip);
		db.Places.Add(place);
		await db.SaveChangesAsync();

		return (trip.Id, place.Id);
	}

	private static CreateTipRequest CreateTipRequest(Guid tripId, Guid? placeId, string content) => new()
	{
		TripId = tripId,
		PlaceId = placeId,
		Content = content
	};

	private async Task<Guid> CreateTipAsync(HttpClient client, Guid tripId, Guid? placeId, string content)
	{
		var response = await client.PostAsJsonAsync($"/api/v1/trips/{tripId}/tips", CreateTipRequest(tripId, placeId, content));
		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var body = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<Guid>(body);
	}

	[Fact]
	public async Task GetByTrip_WithoutToken_Returns401()
	{
		var response = await _client.GetAsync($"/api/v1/trips/{Guid.NewGuid()}/tips");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Create_WithoutToken_Returns401()
	{
		var (tripId, placeId) = await SeedTripAndPlaceAsync();
		var response = await _client.PostAsJsonAsync($"/api/v1/trips/{tripId}/tips", CreateTipRequest(tripId, placeId, "Great stop"));

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Create_WithValidToken_Returns201AndPersistsTip()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();

		var tipId = await CreateTipAsync(authClient, tripId, null, "Visit the bakery before 8am");

		using var scope = _factory.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
		var tip = db.CommunityTips.Single(x => x.Id == tipId);

		tip.TripId.Should().Be(tripId);
		tip.PlaceId.Should().BeNull();
		tip.Content.Should().Be("Visit the bakery before 8am");
		tip.UserId.Should().Be(db.Users.Single(x => x.Email == TestDatabaseSeeder.TestUserEmail).Id);
	}

	[Fact]
	public async Task Create_WithPlaceId_Returns201AndGetShowsPlaceInfo()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();

		await CreateTipAsync(authClient, tripId, placeId, "Order the simit with tea");

		var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/tips?pageNumber=1&pageSize=10");

		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<TipResponse>>(body, _json);

		result.Should().NotBeNull();
		result!.Data.Should().ContainSingle();
		result.Data[0].Place.Should().NotBeNull();
		result.Data[0].Place!.Name.Should().Be("Coastal Bakery");
		result.Data[0].Content.Should().Be("Order the simit with tea");
	}

	[Fact]
	public async Task Upvote_WithoutToken_Returns401()
	{
		var response = await _client.PostAsync($"/api/v1/tips/{Guid.NewGuid()}/upvote", null);

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task Upvote_WithValidToken_Returns204AndUpdatesTip()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();
		var tipId = await CreateTipAsync(authClient, tripId, placeId, "Try the house bread");

		var upvoteResponse = await authClient.PostAsync($"/api/v1/tips/{tipId}/upvote", null);
		upvoteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/tips");
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<TipResponse>>(body, _json);

		result!.Data.Single(x => x.Id == tipId).UpvoteCount.Should().Be(1);
		result.Data.Single(x => x.Id == tipId).IsUpvoted.Should().BeTrue();
	}

	[Fact]
	public async Task RemoveUpvote_WithoutToken_Returns401()
	{
		var response = await _client.DeleteAsync($"/api/v1/tips/{Guid.NewGuid()}/upvote");

		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task RemoveUpvote_WithValidToken_Returns204AndDecrementsCount()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();
		var tipId = await CreateTipAsync(authClient, tripId, placeId, "Try the house bread");

		// First upvote
		await authClient.PostAsync($"/api/v1/tips/{tipId}/upvote", null);

		// Then remove upvote
		var removeResponse = await authClient.DeleteAsync($"/api/v1/tips/{tipId}/upvote");
		removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/tips");
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<TipResponse>>(body, _json);

		result!.Data.Single(x => x.Id == tipId).UpvoteCount.Should().Be(0);
		result.Data.Single(x => x.Id == tipId).IsUpvoted.Should().BeFalse();
	}

	[Fact]
	public async Task RemoveUpvote_WithoutExistingUpvote_Returns404()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();
		var tipId = await CreateTipAsync(authClient, tripId, placeId, "Try the house bread");

		var removeResponse = await authClient.DeleteAsync($"/api/v1/tips/{tipId}/upvote");

		removeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task Delete_WithValidToken_Returns204AndHidesTip()
	{
		var token = await GetAccessTokenAsync(TestDatabaseSeeder.TestUserEmail, TestDatabaseSeeder.TestUserPassword);
		var authClient = CreateAuthenticatedClient(token);
		var (tripId, placeId) = await SeedTripAndPlaceAsync();
		var tipId = await CreateTipAsync(authClient, tripId, placeId, "Go early for fresh pastries");

		var deleteResponse = await authClient.DeleteAsync($"/api/v1/tips/{tipId}");
		deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

		var response = await authClient.GetAsync($"/api/v1/trips/{tripId}/tips");
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var body = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<OmniFlow.Application.Wrappers.PagedResponse<TipResponse>>(body, _json);

		result!.Data.Should().BeEmpty();
	}
}