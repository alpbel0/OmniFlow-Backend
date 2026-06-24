using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class ChangeVerificationEmailRequestValidator : AbstractValidator<ChangeVerificationEmailRequest>
{
	public ChangeVerificationEmailRequestValidator()
	{
		RuleFor(x => x.OldEmail)
			.NotEmpty().WithMessage("Current email is required.")
			.EmailAddress().WithMessage("Current email must be a valid email address.");

		RuleFor(x => x.NewEmail)
			.NotEmpty().WithMessage("New email is required.")
			.EmailAddress().WithMessage("New email must be a valid email address.")
			.NotEqual(x => x.OldEmail, StringComparer.OrdinalIgnoreCase)
			.WithMessage("New email must be different from current email.")
			.WithErrorCode("EMAIL_UNCHANGED");

		RuleFor(x => x.Password)
			.NotEmpty().WithMessage("Password is required.");
	}
}
