using OmniFlow.Application.DTOs.Account;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.IntegrationTests.Setup;

public sealed class TestGoogleTokenValidator : IGoogleTokenValidator
{
	private GoogleTokenPayload _payload = CreateDefaultPayload();
	private ApiException? _exception;
	private readonly Dictionary<string, GoogleTokenPayload> _payloadsByToken = new(StringComparer.Ordinal);

	public int CallCount { get; private set; }

	public Task<GoogleTokenPayload> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
	{
		CallCount++;

		if (_exception is not null)
			throw _exception;

		return Task.FromResult(_payloadsByToken.GetValueOrDefault(idToken, _payload));
	}

	public void UsePayload(
		string email,
		string subject,
		string? name = "Google User",
		string? picture = null)
	{
		_payload = new GoogleTokenPayload
		{
			Email = email,
			EmailVerified = true,
			Name = name,
			Subject = subject,
			Picture = picture
		};
		_exception = null;
		CallCount = 0;
	}

	public void UsePayloadForToken(
		string idToken,
		string email,
		string subject,
		string? name = "Google User",
		string? picture = null)
	{
		_payloadsByToken[idToken] = new GoogleTokenPayload
		{
			Email = email,
			EmailVerified = true,
			Name = name,
			Subject = subject,
			Picture = picture
		};
		_exception = null;
		CallCount = 0;
	}

	public void RejectWith401()
	{
		_exception = new ApiException("Invalid Google id token.", 401);
		CallCount = 0;
	}

	public void Reset()
	{
		_payload = CreateDefaultPayload();
		_exception = null;
		_payloadsByToken.Clear();
		CallCount = 0;
	}

	private static GoogleTokenPayload CreateDefaultPayload()
	{
		return new GoogleTokenPayload
		{
			Email = "google-user@example.com",
			EmailVerified = true,
			Name = "Google User",
			Subject = "google-subject",
			Picture = null
		};
	}
}
