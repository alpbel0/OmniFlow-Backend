using Microsoft.Extensions.Options;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Settings;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Auth;

public class GoogleTokenValidatorTests
{
	[Fact]
	public async Task ValidateAsync_WithValidPayload_ReturnsExpectedPayload()
	{
		var signatureValidator = new FakeGoogleSignatureValidator(
			new GoogleTokenValidationResult(
				"  yigit@example.com  ",
				true,
				"  Yiğit Özgür  ",
				"  google-subject  ",
				"  https://cdn.example.com/avatar.png  "));
		var validator = CreateValidator(signatureValidator);

		var result = await validator.ValidateAsync(" valid-id-token ");

		result.Email.Should().Be("yigit@example.com");
		result.EmailVerified.Should().BeTrue();
		result.Name.Should().Be("Yiğit Özgür");
		result.Subject.Should().Be("google-subject");
		result.Picture.Should().Be("https://cdn.example.com/avatar.png");
		signatureValidator.LastIdToken.Should().Be("valid-id-token");
		signatureValidator.LastAllowedClientIds.Should().Equal("android-client-id", "web-client-id");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task ValidateAsync_WithBlankToken_Throws401(string idToken)
	{
		var validator = CreateValidator(new FakeGoogleSignatureValidator());

		var act = () => validator.ValidateAsync(idToken);

		var exception = await act.Should().ThrowAsync<ApiException>();
		exception.Which.StatusCode.Should().Be(401);
	}

	[Fact]
	public async Task ValidateAsync_WhenGoogleValidationFails_Throws401()
	{
		var validator = CreateValidator(new FakeGoogleSignatureValidator(exception: new ArgumentException("bad token")));

		var act = () => validator.ValidateAsync("invalid-token");

		var exception = await act.Should().ThrowAsync<ApiException>();
		exception.Which.StatusCode.Should().Be(401);
	}

	[Fact]
	public async Task ValidateAsync_WhenEmailIsNotVerified_Throws401()
	{
		var validator = CreateValidator(new FakeGoogleSignatureValidator(
			new GoogleTokenValidationResult("user@example.com", false, "User", "subject", null)));

		var act = () => validator.ValidateAsync("token");

		var exception = await act.Should().ThrowAsync<ApiException>();
		exception.Which.StatusCode.Should().Be(401);
	}

	[Theory]
	[InlineData(null, "subject")]
	[InlineData("", "subject")]
	[InlineData("user@example.com", null)]
	[InlineData("user@example.com", "")]
	public async Task ValidateAsync_WhenRequiredPayloadFieldIsMissing_Throws401(string? email, string? subject)
	{
		var validator = CreateValidator(new FakeGoogleSignatureValidator(
			new GoogleTokenValidationResult(email, true, "User", subject, null)));

		var act = () => validator.ValidateAsync("token");

		var exception = await act.Should().ThrowAsync<ApiException>();
		exception.Which.StatusCode.Should().Be(401);
	}

	[Fact]
	public async Task ValidateAsync_WhenAllowedClientIdsAreMissing_ThrowsInvalidOperationException()
	{
		var validator = CreateValidator(
			new FakeGoogleSignatureValidator(),
			new GoogleAuthSettings { AllowedClientIds = [] });

		var act = () => validator.ValidateAsync("token");

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("GoogleAuth:AllowedClientIds must contain at least one client id.");
	}

	private static GoogleTokenValidator CreateValidator(
		IGoogleJsonWebSignatureValidator signatureValidator,
		GoogleAuthSettings? settings = null)
	{
		return new GoogleTokenValidator(
			signatureValidator,
			Options.Create(settings ?? new GoogleAuthSettings
			{
				AllowedClientIds = ["android-client-id", "web-client-id", "android-client-id", " "]
			}));
	}

	private sealed class FakeGoogleSignatureValidator : IGoogleJsonWebSignatureValidator
	{
		private readonly GoogleTokenValidationResult? _result;
		private readonly Exception? _exception;

		public FakeGoogleSignatureValidator(
			GoogleTokenValidationResult? result = null,
			Exception? exception = null)
		{
			_result = result;
			_exception = exception;
		}

		public string? LastIdToken { get; private set; }
		public IReadOnlyCollection<string>? LastAllowedClientIds { get; private set; }

		public Task<GoogleTokenValidationResult> ValidateAsync(
			string idToken,
			IReadOnlyCollection<string> allowedClientIds,
			CancellationToken cancellationToken = default)
		{
			LastIdToken = idToken;
			LastAllowedClientIds = allowedClientIds;

			if (_exception is not null)
				throw _exception;

			return Task.FromResult(_result ?? new GoogleTokenValidationResult(
				"user@example.com",
				true,
				"User",
				"subject",
				null));
		}
	}
}
