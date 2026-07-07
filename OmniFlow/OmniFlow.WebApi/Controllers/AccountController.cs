using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;
using Microsoft.Extensions.Options;
using ValidationException = OmniFlow.Application.Exceptions.ValidationException;

namespace OmniFlow.WebApi.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
	private readonly IAccountService _accountService;
	private readonly JWTSettings _jwtSettings;
	private readonly IValidator<RegisterRequest> _registerValidator;
	private readonly IValidator<AuthenticationRequest> _loginValidator;
	private readonly IValidator<GoogleLoginRequest> _googleLoginValidator;
	private readonly IValidator<VerifyEmailRequest> _verifyEmailValidator;
	private readonly IValidator<ResendVerificationEmailRequest> _resendVerificationEmailValidator;
	private readonly IValidator<ChangeVerificationEmailRequest> _changeVerificationEmailValidator;
	private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

	public AccountController(
		IAccountService accountService,
		IOptions<JWTSettings> jwtSettings,
		IValidator<RegisterRequest> registerValidator,
		IValidator<AuthenticationRequest> loginValidator,
		IValidator<GoogleLoginRequest> googleLoginValidator,
		IValidator<VerifyEmailRequest> verifyEmailValidator,
		IValidator<ResendVerificationEmailRequest> resendVerificationEmailValidator,
		IValidator<ChangeVerificationEmailRequest> changeVerificationEmailValidator,
		IValidator<ResetPasswordRequest> resetPasswordValidator)
	{
		_accountService = accountService;
		_jwtSettings = jwtSettings.Value;
		_registerValidator = registerValidator;
		_loginValidator = loginValidator;
		_googleLoginValidator = googleLoginValidator;
		_verifyEmailValidator = verifyEmailValidator;
		_resendVerificationEmailValidator = resendVerificationEmailValidator;
		_changeVerificationEmailValidator = changeVerificationEmailValidator;
		_resetPasswordValidator = resetPasswordValidator;
	}

	/// <summary>Register a new user account.</summary>
	[HttpPost("register")]
	[ProducesResponseType(typeof(RegistrationVerificationResponse), StatusCodes.Status202Accepted)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		var validation = await _registerValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		var result = await _accountService.RegisterAsync(request);
		return Accepted(result);
	}

	/// <summary>Authenticate with email and password.</summary>
	[HttpPost("login")]
	[ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
	{
		var validation = await _loginValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		var result = await _accountService.LoginAsync(request);
		return Authenticated(result);
	}

	/// <summary>Authenticate or register with a Google id token.</summary>
	[AllowAnonymous]
	[HttpPost("google")]
	[ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
	{
		var validation = await _googleLoginValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		var result = await _accountService.GoogleLoginAsync(request);
		return Authenticated(result);
	}

	/// <summary>Verify email confirmation token.</summary>
	[HttpPost("verify-email")]
	[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
	{
		var validation = await _verifyEmailValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		await _accountService.VerifyEmailAsync(request);
		return Ok(new MessageResponse { Message = "Email verified successfully." });
	}

	/// <summary>Resend email verification link.</summary>
	[HttpPost("resend-verification")]
	[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationEmailRequest request)
	{
		var validation = await _resendVerificationEmailValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		await _accountService.ResendVerificationEmailAsync(request);
		return Ok(new MessageResponse { Message = "If the account exists, a new verification email has been sent." });
	}

	/// <summary>Change the email address of an unverified account and send a fresh verification link.</summary>
	[HttpPost("change-verification-email")]
	[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> ChangeVerificationEmail([FromBody] ChangeVerificationEmailRequest request)
	{
		var validation = await _changeVerificationEmailValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		await _accountService.ChangeVerificationEmailAsync(request);
		return Ok(new MessageResponse { Message = "Verification email address updated. A new verification email has been sent." });
	}

	/// <summary>
	/// Refresh access token. Web: reads token from HttpOnly cookie, writes new cookie.
	/// Mobile: reads token from request body (X-Platform: mobile), returns new token in body.
	/// </summary>
	[HttpPost("refresh-token")]
	[ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? bodyRequest = null)
	{
		var isMobile = string.Equals(
			Request.Headers["X-Platform"].ToString(),
			"mobile",
			StringComparison.OrdinalIgnoreCase);

		string? token = null;

		if (!isMobile)
		{
			token = Request.Cookies["refreshToken"];
		}

		if (string.IsNullOrWhiteSpace(token))
		{
			token = bodyRequest?.RefreshToken;
		}

		if (string.IsNullOrWhiteSpace(token))
		{
			return BadRequest(new { message = "Refresh token is required." });
		}

		var result = await _accountService.RefreshTokenAsync(token);

		if (isMobile)
		{
			return Ok(result);
		}

		SetRefreshTokenCookie(result.RefreshToken!);
		result.RefreshToken = null;
		return Ok(result);
	}

	/// <summary>Request a password reset email.</summary>
	[HttpPost("forgot-password")]
	[EnableRateLimiting("forgot-password")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
	{
		await _accountService.ForgotPasswordAsync(request.Email);
		return Ok(new { message = "If the email exists, a reset link has been sent." });
	}

	/// <summary>Reset password using token from forgot-password email.</summary>
	[HttpPost("reset-password")]
	[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
	{
		var validation = await _resetPasswordValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		await _accountService.ResetPasswordAsync(request);
		return Ok(new MessageResponse { Message = "Password has been reset successfully." });
	}

	private bool IsMobileRequest() =>
		string.Equals(
			Request.Headers["X-Platform"].ToString(),
			"mobile",
			StringComparison.OrdinalIgnoreCase);

	private IActionResult Authenticated(AuthenticationResponse result)
	{
		SetRefreshTokenCookie(result.RefreshToken!);

		if (!IsMobileRequest())
			result.RefreshToken = null;

		return Ok(result);
	}

	private void SetRefreshTokenCookie(string refreshToken)
	{
		Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
		{
			HttpOnly = true,
			Secure = Request.IsHttps,
			SameSite = SameSiteMode.Strict,
			Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
		});
	}
}
