using OmniFlow.Application.DTOs.Account;

namespace OmniFlow.Application.Interfaces;

public interface IGoogleTokenValidator
{
	Task<GoogleTokenPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
