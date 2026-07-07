using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Settings;

namespace OmniFlow.Infrastructure.Services;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
	private const string InvalidGoogleTokenMessage = "Invalid Google token.";

	private readonly IGoogleJsonWebSignatureValidator _signatureValidator;
	private readonly GoogleAuthSettings _settings;

	public GoogleTokenValidator(
		IGoogleJsonWebSignatureValidator signatureValidator,
		IOptions<GoogleAuthSettings> settings)
	{
		_signatureValidator = signatureValidator;
		_settings = settings.Value;
	}

	public async Task<GoogleTokenPayload> ValidateAsync(
		string idToken,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(idToken))
			throw Unauthorized();

		var allowedClientIds = NormalizeAllowedClientIds(_settings.AllowedClientIds);
		if (allowedClientIds.Count == 0)
			throw new InvalidOperationException("GoogleAuth:AllowedClientIds must contain at least one client id.");

		GoogleTokenValidationResult payload;
		try
		{
			payload = await _signatureValidator.ValidateAsync(
				idToken.Trim(),
				allowedClientIds,
				cancellationToken);
		}
		catch (InvalidJwtException)
		{
			throw Unauthorized();
		}
		catch (ArgumentException)
		{
			throw Unauthorized();
		}

		if (string.IsNullOrWhiteSpace(payload.Email) ||
			string.IsNullOrWhiteSpace(payload.Subject) ||
			!payload.EmailVerified)
		{
			throw Unauthorized();
		}

		return new GoogleTokenPayload
		{
			Email = payload.Email.Trim(),
			EmailVerified = payload.EmailVerified,
			Name = string.IsNullOrWhiteSpace(payload.Name) ? null : payload.Name.Trim(),
			Subject = payload.Subject.Trim(),
			Picture = string.IsNullOrWhiteSpace(payload.Picture) ? null : payload.Picture.Trim()
		};
	}

	private static IReadOnlyCollection<string> NormalizeAllowedClientIds(IEnumerable<string>? clientIds)
	{
		return clientIds?
			.Select(id => id.Trim())
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Distinct(StringComparer.Ordinal)
			.ToArray() ?? Array.Empty<string>();
	}

	private static ApiException Unauthorized()
	{
		return new ApiException(InvalidGoogleTokenMessage, 401);
	}
}
