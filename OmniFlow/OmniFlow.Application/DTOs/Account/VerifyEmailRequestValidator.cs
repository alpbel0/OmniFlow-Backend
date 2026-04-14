using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
	public VerifyEmailRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required.")
			.EmailAddress().WithMessage("Email must be a valid email address.");

		RuleFor(x => x.Token)
			.NotEmpty().WithMessage("Token is required.");
	}
}