using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Infrastructure.Services;

public class AccountService : IAccountService
{
	private const string EmailVerificationPurpose = "email-verification";
	private static readonly TimeSpan VerificationEmailCooldown = TimeSpan.FromSeconds(60);
	private const int DailyVerificationEmailLimit = 5;

	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IApplicationDbContext _context;
	private readonly JWTSettings _jwtSettings;
	private readonly IEmailService _emailService;
	private readonly MailSettings _mailSettings;

	public AccountService(
		UserManager<ApplicationUser> userManager,
		IApplicationDbContext context,
		IOptions<JWTSettings> jwtSettings,
		IOptions<MailSettings> mailSettings,
		IEmailService emailService)
	{
		_userManager = userManager;
		_context = context;
		_jwtSettings = jwtSettings.Value;
		_mailSettings = mailSettings.Value;
		_emailService = emailService;
	}

	public async Task<RegistrationVerificationResponse> RegisterAsync(RegisterRequest request)
	{
		var existingByEmail = await _userManager.FindByEmailAsync(request.Email);
		if (existingByEmail is not null)
			throw new ApiException("Email address is already registered.");

		var existingByUsername = await _userManager.FindByNameAsync(request.Username);
		if (existingByUsername is not null)
			throw new ApiException("Username is already taken.");

		var applicationUser = new ApplicationUser
		{
			Id = Guid.NewGuid(),
			UserName = request.Username,
			Email = request.Email,
			EmailConfirmed = false
		};

		var createResult = await _userManager.CreateAsync(applicationUser, request.Password);
		if (!createResult.Succeeded)
		{
			var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
			throw new ApiException($"Registration failed: {errors}");
		}

		await _userManager.AddToRoleAsync(applicationUser, Roles.Traveler.ToString());

		_context.Users.Add(new User
		{
			Id = applicationUser.Id,
			Username = request.Username,
			Email = request.Email,
			KarmaScore = 0,
			Role = Roles.Traveler,
			IsVerified = false
		});

		var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
		var encodedToken = ToBase64Url(Encoding.UTF8.GetBytes(confirmationToken));
		var verificationUrl = $"{_mailSettings.FrontendVerifyUrl}?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(encodedToken)}";

		await _emailService.SendVerificationEmailAsync(request.Email, verificationUrl);

		await _context.SaveChangesAsync();

		return new RegistrationVerificationResponse
		{
			Message = "Registration successful. Please verify your email address.",
			RequiresEmailVerification = true
		};
	}

	public async Task<AuthenticationResponse> LoginAsync(AuthenticationRequest request)
	{
		var applicationUser = await _userManager.FindByEmailAsync(request.Email);
		if (applicationUser is null)
			throw new ApiException("Invalid email or password.", 401);

		var passwordValid = await _userManager.CheckPasswordAsync(applicationUser, request.Password);
		if (!passwordValid)
			throw new ApiException("Invalid email or password.", 401);

		if (!applicationUser.EmailConfirmed)
			throw new ApiException("Email address is not verified.", 403);

		var domainUser = await _context.Users
			.FirstOrDefaultAsync(u => u.Id == applicationUser.Id);

		if (domainUser is null)
			throw new ApiException("User account is incomplete.", 500);

		if (domainUser.IsSuspended)
			throw new ApiException("Your account has been suspended.", 403);

		var roles = await _userManager.GetRolesAsync(applicationUser);
		var role = roles.FirstOrDefault() ?? Roles.Traveler.ToString();

		var accessToken = GenerateJwtToken(applicationUser, role);
		var (rawRefreshToken, hashedRefreshToken) = GenerateRefreshToken();

		_context.RefreshTokens.Add(new RefreshToken
		{
			UserId = applicationUser.Id,
			TokenHash = hashedRefreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
			CreatedAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();

		return new AuthenticationResponse
		{
			AccessToken = accessToken,
			RefreshToken = rawRefreshToken,
			Id = applicationUser.Id,
			Username = applicationUser.UserName ?? string.Empty,
			Email = applicationUser.Email ?? string.Empty,
			Role = role
		};
	}

	public async Task VerifyEmailAsync(VerifyEmailRequest request)
	{
		var applicationUser = await _userManager.FindByEmailAsync(request.Email);
		if (applicationUser is null)
			throw new ApiException("Invalid verification link.");

		string token;
		try
		{
			var tokenBytes = FromBase64Url(request.Token);
			token = Encoding.UTF8.GetString(tokenBytes);
		}
		catch (Exception)
		{
			throw new ApiException("Invalid verification link.");
		}

		var result = await _userManager.ConfirmEmailAsync(applicationUser, token);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			throw new ApiException(string.IsNullOrWhiteSpace(errors) ? "Email verification failed." : errors);
		}
	}

	public async Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request)
	{
		var applicationUser = await _userManager.FindByEmailAsync(request.Email);
		if (applicationUser is null)
			return;

		if (applicationUser.EmailConfirmed)
			return;

		var now = DateTime.UtcNow;
		await EnsureVerificationEmailDispatchAllowedAsync(applicationUser.Id, request.Email, now);

		var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
		var encodedToken = ToBase64Url(Encoding.UTF8.GetBytes(confirmationToken));
		var verificationUrl = $"{_mailSettings.FrontendVerifyUrl}?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(encodedToken)}";

		await _emailService.SendVerificationEmailAsync(request.Email, verificationUrl);

		_context.Set<EmailVerificationDispatch>().Add(new EmailVerificationDispatch
		{
			UserId = applicationUser.Id,
			Email = request.Email,
			Purpose = EmailVerificationPurpose,
			SentAt = now
		});

		await _context.SaveChangesAsync();
	}

	public async Task ChangeVerificationEmailAsync(ChangeVerificationEmailRequest request)
	{
		var oldEmail = request.OldEmail.Trim();
		var newEmail = request.NewEmail.Trim();

		var applicationUser = await _userManager.FindByEmailAsync(oldEmail);
		if (applicationUser is null)
			throw new ApiException("Unverified account was not found.", 404);

		if (applicationUser.EmailConfirmed)
			throw new ApiException("This account is already verified.");

		var passwordValid = await _userManager.CheckPasswordAsync(applicationUser, request.Password);
		if (!passwordValid)
			throw new ApiException("Invalid email or password.", 401);

		var existingByNewEmail = await _userManager.FindByEmailAsync(newEmail);
		if (existingByNewEmail is not null && existingByNewEmail.Id != applicationUser.Id)
			throw new ApiException("Email address is already registered.");

		var now = DateTime.UtcNow;
		await EnsureVerificationEmailDispatchAllowedAsync(applicationUser.Id, oldEmail, now);

		await using var transaction = await _context.Database.BeginTransactionAsync();

		var setEmailResult = await _userManager.SetEmailAsync(applicationUser, newEmail);
		if (!setEmailResult.Succeeded)
		{
			var errors = string.Join(", ", setEmailResult.Errors.Select(e => e.Description));
			throw new ApiException($"Email update failed: {errors}");
		}

		applicationUser.EmailConfirmed = false;
		var updateResult = await _userManager.UpdateAsync(applicationUser);
		if (!updateResult.Succeeded)
		{
			var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
			throw new ApiException($"Email update failed: {errors}");
		}

		var domainUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == applicationUser.Id);
		if (domainUser is null)
			throw new ApiException("User account is incomplete.", 500);

		domainUser.Email = newEmail;

		var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
		var encodedToken = ToBase64Url(Encoding.UTF8.GetBytes(confirmationToken));
		var verificationUrl = $"{_mailSettings.FrontendVerifyUrl}?email={Uri.EscapeDataString(newEmail)}&token={Uri.EscapeDataString(encodedToken)}";

		await _emailService.SendVerificationEmailAsync(newEmail, verificationUrl);

		_context.Set<EmailVerificationDispatch>().Add(new EmailVerificationDispatch
		{
			UserId = applicationUser.Id,
			Email = newEmail,
			Purpose = EmailVerificationPurpose,
			SentAt = now
		});

		await _context.SaveChangesAsync();
		await transaction.CommitAsync();
	}

	public async Task<AuthenticationResponse> RefreshTokenAsync(string token)
	{
		var hashedToken = HashToken(token);

		var storedToken = await _context.RefreshTokens
			.FirstOrDefaultAsync(rt =>
				rt.TokenHash == hashedToken
				&& rt.RevokedAt == null
				&& rt.ExpiresAt > DateTime.UtcNow);

		if (storedToken is null)
			throw new ApiException("Invalid or expired refresh token.", 401);

		storedToken.RevokedAt = DateTime.UtcNow;

		var applicationUser = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
		if (applicationUser is null)
			throw new ApiException("User not found.", 401);

		var roles = await _userManager.GetRolesAsync(applicationUser);
		var role = roles.FirstOrDefault() ?? Roles.Traveler.ToString();

		var accessToken = GenerateJwtToken(applicationUser, role);
		var (rawRefreshToken, hashedRefreshToken) = GenerateRefreshToken();

		_context.RefreshTokens.Add(new RefreshToken
		{
			UserId = applicationUser.Id,
			TokenHash = hashedRefreshToken,
			ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
			CreatedAt = DateTime.UtcNow
		});

		await _context.SaveChangesAsync();

		return new AuthenticationResponse
		{
			AccessToken = accessToken,
			RefreshToken = rawRefreshToken,
			Id = applicationUser.Id,
			Username = applicationUser.UserName ?? string.Empty,
			Email = applicationUser.Email ?? string.Empty,
			Role = role
		};
	}

	public async Task ForgotPasswordAsync(string email)
	{
		var applicationUser = await _userManager.FindByEmailAsync(email);
		if (applicationUser is null)
			return;

		if (!applicationUser.EmailConfirmed)
			return;

		var now = DateTime.UtcNow;

		var lastDispatch = await _context.Set<EmailVerificationDispatch>()
			.Where(x => x.Email == email && x.Purpose == "password-reset")
			.OrderByDescending(x => x.SentAt)
			.FirstOrDefaultAsync();

		if (lastDispatch is not null && now - lastDispatch.SentAt < TimeSpan.FromSeconds(60))
			throw new ApiException("Please wait before requesting another password reset email.", 429);

		var today = now.Date;
		var dispatchCountToday = await _context.Set<EmailVerificationDispatch>()
			.CountAsync(x => x.Email == email
				&& x.Purpose == "password-reset"
				&& x.SentAt >= today
				&& x.SentAt < today.AddDays(1));

		if (dispatchCountToday >= 5)
			throw new ApiException("You have reached the daily password reset email limit.", 429);

		var existingTokens = await _context.Set<PasswordResetToken>()
			.Where(t => t.UserId == applicationUser.Id && t.UsedAt == null && t.ExpiresAt > now)
			.ToListAsync();

		foreach (var t in existingTokens)
			t.UsedAt = DateTime.UtcNow;

		var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
		var hashedToken = HashToken(rawToken);

		var passwordResetToken = new PasswordResetToken
		{
			UserId = applicationUser.Id,
			TokenHash = hashedToken,
			CreatedAt = now,
			ExpiresAt = now.AddHours(1)
		};

		_context.Set<PasswordResetToken>().Add(passwordResetToken);

		var resetUrl = $"{_mailSettings.FrontendResetUrl}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(rawToken)}";

		await _emailService.SendPasswordResetEmailAsync(email, resetUrl);

		_context.Set<EmailVerificationDispatch>().Add(new EmailVerificationDispatch
		{
			UserId = applicationUser.Id,
			Email = email,
			Purpose = "password-reset",
			SentAt = now
		});

		await _context.SaveChangesAsync();
	}

	public async Task ResetPasswordAsync(ResetPasswordRequest request)
	{
		var applicationUser = await _userManager.FindByEmailAsync(request.Email);
		if (applicationUser is null)
			throw new ApiException("Invalid or expired reset token.");

		var hashedToken = HashToken(request.Token);

		var passwordResetToken = await _context.Set<PasswordResetToken>()
			.Where(t => t.UserId == applicationUser.Id
				&& t.TokenHash == hashedToken
				&& t.UsedAt == null
				&& t.ExpiresAt > DateTime.UtcNow)
			.FirstOrDefaultAsync();

		if (passwordResetToken is null)
			throw new ApiException("Invalid or expired reset token.");

		passwordResetToken.UsedAt = DateTime.UtcNow;

		var result = await _userManager.RemovePasswordAsync(applicationUser);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			throw new ApiException($"Password reset failed: {errors}");
		}

		result = await _userManager.AddPasswordAsync(applicationUser, request.NewPassword);
		if (!result.Succeeded)
		{
			var errors = string.Join(", ", result.Errors.Select(e => e.Description));
			throw new ApiException($"Password reset failed: {errors}");
		}

		await _context.SaveChangesAsync();
	}

	private string GenerateJwtToken(ApplicationUser user, string role)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
			new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
			new Claim(ClaimTypes.Role, role),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		var token = new JwtSecurityToken(
			issuer: _jwtSettings.Issuer,
			audience: _jwtSettings.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
			signingCredentials: credentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}

	private static (string raw, string hashed) GenerateRefreshToken()
	{
		var randomBytes = new byte[64];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomBytes);
		var raw = Convert.ToBase64String(randomBytes);
		return (raw, HashToken(raw));
	}

	private static string HashToken(string token)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
		return Convert.ToBase64String(bytes);
	}

	private async Task EnsureVerificationEmailDispatchAllowedAsync(Guid userId, string email, DateTime now)
	{
		var lastDispatch = await _context.Set<EmailVerificationDispatch>()
			.Where(x => x.Purpose == EmailVerificationPurpose
				&& (x.UserId == userId || x.Email == email))
			.OrderByDescending(x => x.SentAt)
			.FirstOrDefaultAsync();

		if (lastDispatch is not null && now - lastDispatch.SentAt < VerificationEmailCooldown)
			throw new ApiException("Please wait before requesting another verification email.", 429);

		var today = now.Date;
		var dispatchCountToday = await _context.Set<EmailVerificationDispatch>()
			.CountAsync(x => x.Purpose == EmailVerificationPurpose
				&& (x.UserId == userId || x.Email == email)
				&& x.SentAt >= today
				&& x.SentAt < today.AddDays(1));

		if (dispatchCountToday >= DailyVerificationEmailLimit)
			throw new ApiException("You have reached the daily verification email limit.", 429);
	}

	private static string ToBase64Url(byte[] bytes)
	{
		return Convert.ToBase64String(bytes)
			.Replace('+', '-')
			.Replace('/', '_')
			.TrimEnd('=');
	}

	private static byte[] FromBase64Url(string value)
	{
		var base64 = value.Replace('-', '+').Replace('_', '/');

		switch (base64.Length % 4)
		{
			case 2:
				base64 += "==";
				break;
			case 3:
				base64 += "=";
				break;
		}

		return Convert.FromBase64String(base64);
	}
}
