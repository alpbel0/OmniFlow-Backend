namespace OmniFlow.Infrastructure.Services;

public sealed record GoogleTokenValidationResult(
	string? Email,
	bool EmailVerified,
	string? Name,
	string? Subject,
	string? Picture);
