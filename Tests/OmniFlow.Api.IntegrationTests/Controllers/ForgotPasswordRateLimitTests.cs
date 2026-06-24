using OmniFlow.Api.IntegrationTests.Setup;

namespace OmniFlow.Api.IntegrationTests.Controllers;

[Collection("Integration")]
public class ForgotPasswordRateLimitTests
{
    [Fact]
    public async Task ForgotPassword_SixRequestsFromSameIp_Returns429()
    {
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var allowedResponse = await client.PostAsJsonAsync(
                "/api/account/forgot-password",
                new { email = $"unknown-{attempt}-{Guid.NewGuid():N}@test.com" });

            allowedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var blockedResponse = await client.PostAsJsonAsync(
            "/api/account/forgot-password",
            new { email = $"unknown-blocked-{Guid.NewGuid():N}@test.com" });

        blockedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
