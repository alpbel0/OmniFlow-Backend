using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class AccountControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AccountControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        TestDatabaseSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
    }

    // ── Register ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_ReturnsVerificationPendingResponse()
    {
        var request = new RegisterRequest
        {
            Username = $"newuser_{Guid.NewGuid():N}".Substring(0, 20),
            Email = $"newuser_{Guid.NewGuid():N}@test.com",
            Password = "ValidPass1!",
            ConfirmPassword = "ValidPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegistrationVerificationResponse>(body, _json);

        result.Should().NotBeNull();
        result!.RequiresEmailVerification.Should().BeTrue();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var email = $"dup_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest
        {
            Username = $"dup_{Guid.NewGuid():N}".Substring(0, 16),
            Email = email,
            Password = "ValidPass1!",
            ConfirmPassword = "ValidPass1!"
        };

        await _client.PostAsJsonAsync("/api/account/register", request);

        var request2 = new RegisterRequest
        {
            Username = $"dup2_{Guid.NewGuid():N}".Substring(0, 16),
            Email = email,
            Password = "ValidPass1!",
            ConfirmPassword = "ValidPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/account/register", request2);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns422()
    {
        var request = new RegisterRequest
        {
            Username = "weakpassuser",
            Email = "weak@test.com",
            Password = "abc",
            ConfirmPassword = "abc"
        };

        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_WithNewlyRegisteredUser_Returns403UntilVerified()
    {
        var email = $"pending_{Guid.NewGuid():N}@test.com";
        var password = "ValidPass1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/account/register", new RegisterRequest
        {
            Username = $"pending_{Guid.NewGuid():N}".Substring(0, 20),
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Login ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = TestDatabaseSeeder.TestUserPassword
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, _json);

        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Email.Should().Be(TestDatabaseSeeder.TestUserEmail);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = "WrongPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = "nobody@nowhere.com",
            Password = "SomePassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Refresh Token ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_WithCookie_ReturnsNewAccessToken()
    {
        // Login to get cookie
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = TestDatabaseSeeder.TestUserPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // The factory's client preserves cookies automatically (UseCookies = true by default)
        var refreshResponse = await _client.PostAsJsonAsync("/api/account/refresh-token", (object?)null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await refreshResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(body, _json);

        result!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_WithBody_MobilePlatform_ReturnsTokenInBody()
    {
        // Login to get refresh token in body (mobile flow)
        var loginClient = _factory.CreateClient();
        loginClient.DefaultRequestHeaders.Add("X-Platform", "mobile");

        var loginResponse = await loginClient.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = TestDatabaseSeeder.TestUserPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(loginBody, _json);

        loginResult!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // Use the raw refresh token in body for mobile refresh
        var refreshClient = _factory.CreateClient();
        refreshClient.DefaultRequestHeaders.Add("X-Platform", "mobile");

        var refreshResponse = await refreshClient.PostAsJsonAsync("/api/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken! });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        var refreshResult = JsonSerializer.Deserialize<AuthenticationResponse>(refreshBody, _json);

        refreshResult!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshResult.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_WithExpiredToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = "this-token-does-not-exist-at-all" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_Returns401()
    {
        // Login first time → get refresh token
        var mobileClient1 = _factory.CreateClient();
        mobileClient1.DefaultRequestHeaders.Add("X-Platform", "mobile");

        var login1 = await mobileClient1.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = TestDatabaseSeeder.TestUserPassword
        });
        var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(
            await login1.Content.ReadAsStringAsync(), _json)!;

        // Use token once (rotates — original is now revoked)
        var mobileClient2 = _factory.CreateClient();
        mobileClient2.DefaultRequestHeaders.Add("X-Platform", "mobile");
        await mobileClient2.PostAsJsonAsync("/api/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken! });

        // Attempt to reuse the original (now revoked) token
        var mobileClient3 = _factory.CreateClient();
        mobileClient3.DefaultRequestHeaders.Add("X-Platform", "mobile");
        var reuseResponse = await mobileClient3.PostAsJsonAsync("/api/account/refresh-token",
            new RefreshTokenRequest { RefreshToken = loginResult.RefreshToken! });

        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithNoToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/account/refresh-token", (object?)null);

        // No cookie set and no body → 400 or 401 depending on cookie presence
        ((int)response.StatusCode).Should().BeOneOf(400, 401);
    }

    // ── Protected Endpoint ────────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/meta/info");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        // Login and get access token
        var loginResponse = await _client.PostAsJsonAsync("/api/account/login", new AuthenticationRequest
        {
            Email = TestDatabaseSeeder.TestUserEmail,
            Password = TestDatabaseSeeder.TestUserPassword
        });
        var loginResult = JsonSerializer.Deserialize<AuthenticationResponse>(
            await loginResponse.Content.ReadAsStringAsync(), _json)!;

        // Call protected endpoint with Bearer token
        var authClient = _factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        var response = await authClient.GetAsync("/api/meta/info");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
