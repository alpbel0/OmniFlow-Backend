using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
	public GoogleLoginRequestValidator()
	{
		RuleFor(x => x.IdToken)
			.NotEmpty()
				.WithMessage("Google id token is required.")
				.WithErrorCode("GOOGLE_ID_TOKEN_REQUIRED");
	}
}
