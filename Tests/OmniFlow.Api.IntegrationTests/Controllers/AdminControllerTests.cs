using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class AdminControllerTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;
	private readonly CustomWebApplicationFactory _factory;

	public AdminControllerTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
		_client = factory.CreateClient();

		using var scope = factory.Services.CreateScope();
		TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
	}

	[Fact]
	public async Task GetStats_AsAdmin_ReturnsDashboardContract()
	{
		var adminClient = await CreateAuthenticatedClientAsync(
			TestDatabaseSeeder.AdminEmail,
			TestDatabaseSeeder.AdminPassword);

		var response = await adminClient.GetAsync("/api/v1/admin/stats");

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		body.RootElement.TryGetProperty("totalUsers", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("newUsersToday", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("newUsersThisWeek", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("totalTrips", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("newTripsToday", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("totalPosts", out _).Should().BeTrue();
		body.RootElement.TryGetProperty("newPostsToday", out _).Should().BeTrue();
	}

	[Fact]
	public async Task GetStats_AsTraveler_Returns403()
	{
		var travelerClient = await CreateAuthenticatedClientAsync(
			TestDatabaseSeeder.TestUserEmail,
			TestDatabaseSeeder.TestUserPassword);

		var response = await travelerClient.GetAsync("/api/v1/admin/stats");

		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
	{
		var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
		{
			Email = email,
			Password = password
		});
		loginResponse.EnsureSuccessStatusCode();

		var result = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
		var client = _factory.CreateClient();
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);
		return client;
	}
}
