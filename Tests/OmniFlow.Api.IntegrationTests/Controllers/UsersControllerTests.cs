using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Interfaces;

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
}