using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class ResendVerificationEmailRequestValidator : AbstractValidator<ResendVerificationEmailRequest>
{
	public ResendVerificationEmailRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required.")
			.EmailAddress().WithMessage("Email must be a valid email address.");
	}
}