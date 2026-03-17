using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OmniFlow.Api.IntegrationTests.Setup;

namespace OmniFlow.Api.IntegrationTests.Auth;

/// <summary>
/// Verifies that JWT TokenValidationParameters in Program.cs are configured
/// exactly as required by Task 3.5:
///   ValidateIssuer = true
///   ValidateAudience = true
///   ValidateLifetime = true
///   ValidateIssuerSigningKey = true
///   ClockSkew = TimeSpan.Zero
/// </summary>
public class JwtConfigurationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ProtectedUrl = "/api/meta/info";

    // These must match appsettings.json JWTSettings
    private const string ValidKey = "CHANGE_ME_IN_PRODUCTION_USE_A_LONG_RANDOM_SECRET_KEY_32_CHARS_MIN";
    private const string ValidIssuer = "OmniFlow";
    private const string ValidAudience = "OmniFlowClient";

    private readonly HttpClient _client;

    public JwtConfigurationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static string BuildToken(
        string key = ValidKey,
        string issuer = ValidIssuer,
        string audience = ValidAudience,
        int expiresInSeconds = 60,
        bool expired = false)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var notBefore = expired ? now.AddSeconds(-120) : now;
        var expires = expired ? now.AddSeconds(-60) : now.AddSeconds(expiresInSeconds);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            notBefore: notBefore,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpRequestMessage BuildRequest(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ProtectedUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    // ── ValidateIssuer ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateIssuer_WrongIssuer_Returns401()
    {
        var token = BuildToken(issuer: "WrongIssuer");

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateIssuer_CorrectIssuer_Returns200()
    {
        var token = BuildToken();

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── ValidateAudience ──────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAudience_WrongAudience_Returns401()
    {
        var token = BuildToken(audience: "WrongAudience");

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ValidateIssuerSigningKey ───────────────────────────────────────────────

    [Fact]
    public async Task ValidateIssuerSigningKey_WrongKey_Returns401()
    {
        // Must be 32+ chars to satisfy SymmetricSecurityKey minimum key size
        var token = BuildToken(key: "WRONG_KEY_WRONG_KEY_WRONG_KEY_WRONG_KEY_!!!");

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ValidateLifetime + ClockSkew = Zero ───────────────────────────────────

    [Fact]
    public async Task ValidateLifetime_ExpiredToken_Returns401_WithNoClockSkewTolerance()
    {
        // Token expired 60 seconds ago — without ClockSkew tolerance this must be 401.
        var token = BuildToken(expired: true);

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ValidateLifetime_ValidToken_Returns200()
    {
        var token = BuildToken(expiresInSeconds: 300);

        var response = await _client.SendAsync(BuildRequest(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── No token at all ───────────────────────────────────────────────────────

    [Fact]
    public async Task NoToken_Returns401()
    {
        var response = await _client.GetAsync(ProtectedUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
