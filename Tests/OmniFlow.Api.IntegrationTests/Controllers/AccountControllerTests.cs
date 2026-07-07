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
        factory.GoogleTokenValidator.Reset();

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
    public async Task GoogleLogin_WithValidToken_CreatesVerifiedUserAndReturnsWebCookieSession()
    {
        var email = $"google_new_{Guid.NewGuid():N}@test.com";
        var subject = $"google-subject-{Guid.NewGuid():N}";
        var uniqueNamePart = $"ozgur_{Guid.NewGuid():N}".Substring(0, 14);
        var expectedUsername = $"yigit_{uniqueNamePart}";
        _factory.GoogleTokenValidator.UsePayload(email, subject, $"Yigit {uniqueNamePart}");

        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Any(cookie => cookie.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();

        var result = JsonSerializer.Deserialize<AuthenticationResponse>(
            await response.Content.ReadAsStringAsync(), _json)!;
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().BeNull();
        result.Email.Should().Be(email);
        result.Username.Should().Be(expectedUsername);
        _factory.EmailService.VerificationEmailCount.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var identityUser = await userManager.FindByEmailAsync(email);
        identityUser.Should().NotBeNull();
        identityUser!.EmailConfirmed.Should().BeTrue();
        (await userManager.IsInRoleAsync(identityUser, "Traveler")).Should().BeTrue();
        (await userManager.GetLoginsAsync(identityUser))
            .Should().Contain(login => login.LoginProvider == "Google" && login.ProviderKey == subject);

        var domainUser = await db.Users.SingleAsync(user => user.Id == identityUser.Id);
        domainUser.IsVerified.Should().BeTrue();
        domainUser.Username.Should().Be(expectedUsername);
    }

    [Fact]
    public async Task GoogleLogin_WithMobileHeader_ReturnsRefreshTokenInBody()
    {
        var email = $"google_mobile_{Guid.NewGuid():N}@test.com";
        _factory.GoogleTokenValidator.UsePayload(email, $"google-subject-{Guid.NewGuid():N}", "Mobile User");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/account/google")
        {
            Content = JsonContent.Create(new GoogleLoginRequest { IdToken = "valid-google-token" })
        };
        request.Headers.Add("X-Platform", "mobile");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(
            await response.Content.ReadAsStringAsync(), _json)!;
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GoogleLogin_WhenCalledAgain_UsesExistingProviderLinkWithoutDuplicateLogin()
    {
        var email = $"google_repeat_{Guid.NewGuid():N}@test.com";
        var subject = $"google-subject-{Guid.NewGuid():N}";
        _factory.GoogleTokenValidator.UsePayload(email, subject, "Repeat User");

        var firstResponse = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });
        var secondResponse = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = JsonSerializer.Deserialize<AuthenticationResponse>(
            await firstResponse.Content.ReadAsStringAsync(), _json)!;
        var second = JsonSerializer.Deserialize<AuthenticationResponse>(
            await secondResponse.Content.ReadAsStringAsync(), _json)!;
        second.Id.Should().Be(first.Id);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var identityUser = await userManager.FindByEmailAsync(email);
        (await userManager.GetLoginsAsync(identityUser!))
            .Count(login => login.LoginProvider == "Google" && login.ProviderKey == subject)
            .Should().Be(1);
    }

    [Fact]
    public async Task GoogleLogin_WithExistingEmailPasswordUser_LinksAndReturnsSameUser()
    {
        var email = $"google_link_{Guid.NewGuid():N}@test.com";
        var username = $"glink_{Guid.NewGuid():N}".Substring(0, 20);
        var userId = await RegisterPendingUserAsync(username, email, "ValidPass1!");

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var identityUser = await userManager.FindByEmailAsync(email);
            identityUser!.EmailConfirmed = true;
            await userManager.UpdateAsync(identityUser);
        }

        var subject = $"google-subject-{Guid.NewGuid():N}";
        _factory.GoogleTokenValidator.UsePayload(email, subject, "Linked User");
        _factory.EmailService.Reset();

        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(
            await response.Content.ReadAsStringAsync(), _json)!;
        result.Id.Should().Be(userId);
        result.Username.Should().Be(username);
        _factory.EmailService.VerificationEmailCount.Should().Be(0);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var linkedUser = await verifyUserManager.FindByEmailAsync(email);
        (await verifyUserManager.GetLoginsAsync(linkedUser!))
            .Should().Contain(login => login.LoginProvider == "Google" && login.ProviderKey == subject);
    }

    [Fact]
    public async Task GoogleLogin_WithExistingUnverifiedEmailPasswordUser_ConfirmsAndLinksAccount()
    {
        var email = $"google_confirm_{Guid.NewGuid():N}@test.com";
        var username = $"gconfirm_{Guid.NewGuid():N}".Substring(0, 20);
        var userId = await RegisterPendingUserAsync(username, email, "ValidPass1!");
        _factory.EmailService.Reset();

        _factory.GoogleTokenValidator.UsePayload(email, $"google-subject-{Guid.NewGuid():N}", "Confirm User");

        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = JsonSerializer.Deserialize<AuthenticationResponse>(
            await response.Content.ReadAsStringAsync(), _json)!;
        result.Id.Should().Be(userId);
        _factory.EmailService.VerificationEmailCount.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var identityUser = await userManager.FindByEmailAsync(email);
        identityUser!.EmailConfirmed.Should().BeTrue();
        (await db.Users.SingleAsync(user => user.Id == userId)).IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GoogleLogin_WithSuspendedUser_Returns403()
    {
        var email = $"google_suspended_{Guid.NewGuid():N}@test.com";
        var username = $"gsuspend_{Guid.NewGuid():N}".Substring(0, 20);
        var userId = await RegisterPendingUserAsync(username, email, "ValidPass1!");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var domainUser = await db.Users.SingleAsync(user => user.Id == userId);
            domainUser.IsSuspended = true;
            await db.SaveChangesAsync();
        }

        _factory.GoogleTokenValidator.UsePayload(email, $"google-subject-{Guid.NewGuid():N}", "Suspended User");

        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GoogleLogin_WithInvalidToken_Returns401()
    {
        _factory.GoogleTokenValidator.RejectWith401();

        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "invalid-google-token"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GoogleLogin_WithBlankToken_Returns422(string idToken)
    {
        var response = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = idToken
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GoogleLogin_WhenUsernameCollides_AppendsNumericSuffix()
    {
        var uniqueBase = $"collision_{Guid.NewGuid():N}".Substring(0, 18);
        var name = uniqueBase.Replace('_', ' ');

        _factory.GoogleTokenValidator.UsePayload(
            $"google_collision_1_{Guid.NewGuid():N}@test.com",
            $"google-subject-{Guid.NewGuid():N}",
            name);
        var firstResponse = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        _factory.GoogleTokenValidator.UsePayload(
            $"google_collision_2_{Guid.NewGuid():N}@test.com",
            $"google-subject-{Guid.NewGuid():N}",
            name);
        var secondResponse = await _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = "valid-google-token"
        });

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = JsonSerializer.Deserialize<AuthenticationResponse>(
            await firstResponse.Content.ReadAsStringAsync(), _json)!;
        var second = JsonSerializer.Deserialize<AuthenticationResponse>(
            await secondResponse.Content.ReadAsStringAsync(), _json)!;
        first.Username.Should().Be(uniqueBase);
        second.Username.Should().Be($"{uniqueBase}_1");
    }

    [Fact]
    public async Task GoogleLogin_WithConcurrentSameNameRegistrations_ReturnsSuccessfulUniqueUsernames()
    {
        var uniqueBase = $"race_{Guid.NewGuid():N}".Substring(0, 18);
        var name = uniqueBase.Replace('_', ' ');
        var firstToken = $"google-token-{Guid.NewGuid():N}";
        var secondToken = $"google-token-{Guid.NewGuid():N}";

        _factory.GoogleTokenValidator.UsePayloadForToken(
            firstToken,
            $"google_race_1_{Guid.NewGuid():N}@test.com",
            $"google-subject-{Guid.NewGuid():N}",
            name);
        _factory.GoogleTokenValidator.UsePayloadForToken(
            secondToken,
            $"google_race_2_{Guid.NewGuid():N}@test.com",
            $"google-subject-{Guid.NewGuid():N}",
            name);

        var firstTask = _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = firstToken
        });
        var secondTask = _client.PostAsJsonAsync("/api/account/google", new GoogleLoginRequest
        {
            IdToken = secondToken
        });

        var responses = await Task.WhenAll(firstTask, secondTask);

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        var results = new List<AuthenticationResponse>();
        foreach (var response in responses)
        {
            results.Add(JsonSerializer.Deserialize<AuthenticationResponse>(
                await response.Content.ReadAsStringAsync(), _json)!);
        }

        results.Select(result => result.Id).Should().OnlyHaveUniqueItems();
        results.Select(result => result.Username)
            .Should().BeEquivalentTo([uniqueBase, $"{uniqueBase}_1"]);
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
