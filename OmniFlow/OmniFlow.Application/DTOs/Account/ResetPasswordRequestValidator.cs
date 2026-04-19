using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty().WithMessage("Email is required.")
			.EmailAddress().WithMessage("Email must be a valid email address.");

		RuleFor(x => x.Token)
			.NotEmpty().WithMessage("Token is required.");

		RuleFor(x => x.NewPassword)
			.NotEmpty().WithMessage("New password is required.")
			.MinimumLength(8).WithMessage("Password must be at least 8 characters.")
			.Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
			.Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
			.Matches("[0-9]").WithMessage("Password must contain at least one digit.");
	}
}