using Google.Apis.Auth;

namespace OmniFlow.Infrastructure.Services;

internal sealed class GoogleJsonWebSignatureValidator : IGoogleJsonWebSignatureValidator
{
	public async Task<GoogleTokenValidationResult> ValidateAsync(
		string idToken,
		IReadOnlyCollection<string> allowedClientIds,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var payload = await GoogleJsonWebSignature.ValidateAsync(
			idToken,
			new GoogleJsonWebSignature.ValidationSettings
			{
				Audience = allowedClientIds
			});

		return new GoogleTokenValidationResult(
			payload.Email,
			payload.EmailVerified,
			payload.Name,
			payload.Subject,
			payload.Picture);
	}
}
