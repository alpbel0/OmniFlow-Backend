using OmniFlow.Api.IntegrationTests.Setup;

namespace OmniFlow.Api.IntegrationTests.Cors;

/// <summary>
/// Verifies the CORS policy configured in Program.cs (Task 3.5):
///   - AllowedOrigins: http://localhost:3000, https://omniflow.app
///   - AllowAnyHeader, AllowAnyMethod, AllowCredentials
/// Uses the public /api/meta/health endpoint to avoid auth interference.
/// </summary>
public class CorsMiddlewareTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string HealthUrl = "/api/meta/health";
    private readonly HttpClient _client;

    public CorsMiddlewareTests(CustomWebApplicationFactory factory)
    {
        // Don't follow auto-redirects so we can inspect raw CORS headers
        _client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    // ── Preflight (OPTIONS) from allowed origin ────────────────────────────────

    [Fact]
    public async Task Preflight_FromLocalhostOrigin_ReturnsAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, HealthUrl);
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        values.Should().NotBeNull();
        values!.Should().Contain("http://localhost:3000");
    }

    [Fact]
    public async Task Preflight_FromProductionOrigin_ReturnsAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, HealthUrl);
        request.Headers.Add("Origin", "https://omniflow.app");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        values.Should().NotBeNull();
        values!.Should().Contain("https://omniflow.app");
    }

    [Fact]
    public async Task Preflight_AllowCredentials_HeaderPresent()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, HealthUrl);
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var values);
        values.Should().NotBeNull();
        values!.Should().Contain("true");
    }

    [Fact]
    public async Task Preflight_AllowCustomHeader_HeaderPresent()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, HealthUrl);
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "X-Platform");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Headers", out var values);
        values.Should().NotBeNull();
    }

    // ── Actual GET with Origin header ────────────────────────────────────────

    [Fact]
    public async Task ActualRequest_FromAllowedOrigin_ReturnsAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, HealthUrl);
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request);

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        values.Should().NotBeNull();
        values!.Should().Contain("http://localhost:3000");
    }

    [Fact]
    public async Task ActualRequest_FromDisallowedOrigin_DoesNotReturnAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, HealthUrl);
        request.Headers.Add("Origin", "http://evil.com");

        var response = await _client.SendAsync(request);

        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values);
        // Either null or should not contain the disallowed origin
        if (values is not null)
            values.Should().NotContain("http://evil.com");
    }
}
