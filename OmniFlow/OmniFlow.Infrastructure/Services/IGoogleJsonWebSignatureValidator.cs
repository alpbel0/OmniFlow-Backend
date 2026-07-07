namespace OmniFlow.Infrastructure.Services;

public interface IGoogleJsonWebSignatureValidator
{
	Task<GoogleTokenValidationResult> ValidateAsync(
		string idToken,
		IReadOnlyCollection<string> allowedClientIds,
		CancellationToken cancellationToken = default);
}
