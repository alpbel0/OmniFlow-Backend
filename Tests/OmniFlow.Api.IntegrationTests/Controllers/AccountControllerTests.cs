using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Api.IntegrationTests.Setup;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Models;

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
        factory.EmailService.Reset();

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

    [Fact]
    public async Task ChangeVerificationEmail_WithValidPendingUser_UpdatesIdentityAndDomainEmails()
    {
        var oldEmail = $"change_old_{Guid.NewGuid():N}@test.com";
        var newEmail = $"change_new_{Guid.NewGuid():N}@test.com";
        var username = $"change_{Guid.NewGuid():N}".Substring(0, 20);
        const string password = "ValidPass1!";

        await RegisterPendingUserAsync(username, oldEmail, password);

        var response = await _client.PostAsJsonAsync("/api/account/change-verification-email", new
        {
            oldEmail,
            newEmail,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        (await userManager.FindByEmailAsync(oldEmail)).Should().BeNull();

        var updatedIdentityUser = await userManager.FindByEmailAsync(newEmail);
        updatedIdentityUser.Should().NotBeNull();
        updatedIdentityUser!.EmailConfirmed.Should().BeFalse();

        var updatedDomainUser = await dbContext.Users.FindAsync(updatedIdentityUser.Id);
        updatedDomainUser.Should().NotBeNull();
        updatedDomainUser!.Email.Should().Be(newEmail);

        dbContext.EmailVerificationDispatches
            .Any(x => x.UserId == updatedIdentityUser.Id
                && x.Email == newEmail
                && x.Purpose == "email-verification")
            .Should().BeTrue();
    }

    [Fact]
    public async Task ChangeVerificationEmail_WithSameEmail_Returns422()
    {
        var email = $"same_{Guid.NewGuid():N}@test.com";

        var response = await _client.PostAsJsonAsync("/api/account/change-verification-email", new
        {
            oldEmail = email,
            newEmail = email,
            password = "ValidPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ChangeVerificationEmail_WithWrongPassword_Returns401()
    {
        var oldEmail = $"wrong_pw_{Guid.NewGuid():N}@test.com";
        var newEmail = $"wrong_pw_new_{Guid.NewGuid():N}@test.com";
        var username = $"wrongpw_{Guid.NewGuid():N}".Substring(0, 20);

        await RegisterPendingUserAsync(username, oldEmail, "ValidPass1!");

        var response = await _client.PostAsJsonAsync("/api/account/change-verification-email", new
        {
            oldEmail,
            newEmail,
            password = "WrongPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeVerificationEmail_WhenNewEmailExists_Returns400()
    {
        var oldEmail = $"dup_change_{Guid.NewGuid():N}@test.com";
        var existingEmail = $"dup_existing_{Guid.NewGuid():N}@test.com";
        var username = $"dupchange_{Guid.NewGuid():N}".Substring(0, 20);
        const string password = "ValidPass1!";

        await RegisterPendingUserAsync(username, oldEmail, password);
        await RegisterPendingUserAsync($"dupexist_{Guid.NewGuid():N}".Substring(0, 20), existingEmail, password);

        var response = await _client.PostAsJsonAsync("/api/account/change-verification-email", new
        {
            oldEmail,
            newEmail = existingEmail,
            password
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeVerificationEmail_WhenCooldownActive_Returns429()
    {
        var oldEmail = $"cooldown_{Guid.NewGuid():N}@test.com";
        var newEmail = $"cooldown_new_{Guid.NewGuid():N}@test.com";
        var username = $"cooldown_{Guid.NewGuid():N}".Substring(0, 20);
        const string password = "ValidPass1!";

        var userId = await RegisterPendingUserAsync(username, oldEmail, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            dbContext.EmailVerificationDispatches.Add(new EmailVerificationDispatch
            {
                UserId = userId,
                Email = oldEmail,
                Purpose = "email-verification",
                SentAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/account/change-verification-email", new
        {
            oldEmail,
            newEmail,
            password
        });

        response.StatusCode.Should().Be((HttpStatusCode)429);
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

    [Fact]
    public async Task ForgotPassword_WithUnconfirmedUser_SendsResetEmailAndPersistsDispatch()
    {
        var email = $"forgot_pending_{Guid.NewGuid():N}@test.com";
        var userId = await RegisterPendingUserAsync(
            $"forgot_{Guid.NewGuid():N}"[..20],
            email,
            "ValidPass1!");

        var response = await _client.PostAsJsonAsync("/api/account/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.EmailService.LastPasswordResetEmail.Should().Be(email);
        _factory.EmailService.LastPasswordResetUrl.Should().NotBeNullOrWhiteSpace();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.PasswordResetTokens.Any(x => x.UserId == userId && x.UsedAt == null).Should().BeTrue();
        db.EmailVerificationDispatches.Any(x =>
            x.UserId == userId && x.Email == email && x.Purpose == "password-reset").Should().BeTrue();
    }

    [Fact]
    public async Task ForgotPassword_WhenSmtpFails_Returns503WithoutConsumingQuota()
    {
        var email = $"forgot_smtp_{Guid.NewGuid():N}@test.com";
        var userId = await RegisterPendingUserAsync(
            $"smtp_{Guid.NewGuid():N}"[..20],
            email,
            "ValidPass1!");
        _factory.EmailService.FailPasswordResetDelivery = true;

        var response = await _client.PostAsJsonAsync("/api/account/forgot-password", new { email });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        db.PasswordResetTokens.Any(x => x.UserId == userId).Should().BeFalse();
        db.EmailVerificationDispatches.Any(x =>
            x.UserId == userId && x.Purpose == "password-reset").Should().BeFalse();
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ConfirmsEmailRevokesSessionsAndConsumesToken()
    {
        var email = $"reset_pending_{Guid.NewGuid():N}@test.com";
        var userId = await RegisterPendingUserAsync(
            $"reset_{Guid.NewGuid():N}"[..20],
            email,
            "ValidPass1!");

        using (var setupScope = _factory.Services.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = $"test-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
            await db.SaveChangesAsync();
        }

        var forgotResponse = await _client.PostAsJsonAsync("/api/account/forgot-password", new { email });
        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetUrl = new Uri(_factory.EmailService.LastPasswordResetUrl!);
        var token = QueryHelpers.ParseQuery(resetUrl.Query)["token"].ToString();

        var resetResponse = await _client.PostAsJsonAsync("/api/account/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Token = token,
            NewPassword = "NewValidPass2!"
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verifyScope = _factory.Services.CreateScope();
        var userManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbAfter = verifyScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var identityUser = await userManager.FindByEmailAsync(email);
        identityUser.Should().NotBeNull();
        identityUser!.EmailConfirmed.Should().BeTrue();
        (await userManager.CheckPasswordAsync(identityUser, "NewValidPass2!")).Should().BeTrue();

        var domainUser = await dbAfter.Users.FindAsync(userId);
        domainUser!.IsVerified.Should().BeFalse();
        dbAfter.PasswordResetTokens.Single(x => x.UserId == userId).UsedAt.Should().NotBeNull();
        dbAfter.RefreshTokens
            .Where(x => x.UserId == userId)
            .All(x => x.RevokedAt != null)
            .Should().BeTrue();

        var reusedTokenResponse = await _client.PostAsJsonAsync("/api/account/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Token = token,
            NewPassword = "AnotherValid3!"
        });
        reusedTokenResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WhenIdentityRejectsPassword_RollsBackAllChanges()
    {
        var email = $"reset_rollback_{Guid.NewGuid():N}@test.com";
        const string originalPassword = "ValidPass1!";
        var userId = await RegisterPendingUserAsync(
            $"rollback_{Guid.NewGuid():N}"[..20],
            email,
            originalPassword);

        var forgotResponse = await _client.PostAsJsonAsync("/api/account/forgot-password", new { email });
        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetUrl = new Uri(_factory.EmailService.LastPasswordResetUrl!);
        var token = QueryHelpers.ParseQuery(resetUrl.Query)["token"].ToString();

        var resetResponse = await _client.PostAsJsonAsync("/api/account/reset-password", new ResetPasswordRequest
        {
            Email = email,
            Token = token,
            NewPassword = "NoSpecial123"
        });

        resetResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var identityUser = await userManager.FindByEmailAsync(email);
        identityUser!.EmailConfirmed.Should().BeFalse();
        (await userManager.CheckPasswordAsync(identityUser, originalPassword)).Should().BeTrue();
        db.PasswordResetTokens.Single(x => x.UserId == userId).UsedAt.Should().BeNull();
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

    private async Task<Guid> RegisterPendingUserAsync(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/account/register", new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }
}
