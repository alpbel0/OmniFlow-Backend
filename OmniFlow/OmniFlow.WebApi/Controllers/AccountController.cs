using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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

	public AccountController(
		IAccountService accountService,
		IOptions<JWTSettings> jwtSettings,
		IValidator<RegisterRequest> registerValidator,
		IValidator<AuthenticationRequest> loginValidator)
	{
		_accountService = accountService;
		_jwtSettings = jwtSettings.Value;
		_registerValidator = registerValidator;
		_loginValidator = loginValidator;
	}

	/// <summary>Register a new user account.</summary>
	[HttpPost("register")]
	[ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		var validation = await _registerValidator.ValidateAsync(request);
		if (!validation.IsValid)
			throw new ValidationException(validation.Errors);

		var result = await _accountService.RegisterAsync(request);
		SetRefreshTokenCookie(result.RefreshToken!);

		if (!IsMobileRequest())
			result.RefreshToken = null;

		return Ok(result);
	}

	/// <summary>Authenticate with email and password.</summary>
	[HttpPost("login")]
	[ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Login([FromBody] AuthenticationRequest request)
	{
		var result = await _accountService.LoginAsync(request);
		SetRefreshTokenCookie(result.RefreshToken!);

		if (!IsMobileRequest())
			result.RefreshToken = null;

		return Ok(result);
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

	/// <summary>Request a password reset email (placeholder — mail service not implemented in MVP).</summary>
	[HttpPost("forgot-password")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
	{
		await _accountService.ForgotPasswordAsync(request.Email);
		return Ok(new { message = "If the email exists, a reset link has been sent." });
	}

	private bool IsMobileRequest() =>
		string.Equals(
			Request.Headers["X-Platform"].ToString(),
			"mobile",
			StringComparison.OrdinalIgnoreCase);

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
