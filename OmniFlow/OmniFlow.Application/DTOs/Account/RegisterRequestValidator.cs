using FluentValidation;

namespace OmniFlow.Application.DTOs.Account;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator()
	{
		RuleFor(x => x.Username)
			.NotEmpty()
				.WithMessage("Username is required.")
				.WithErrorCode("USERNAME_REQUIRED")
			.MinimumLength(3)
				.WithMessage("Username must be at least 3 characters.")
				.WithErrorCode("USERNAME_TOO_SHORT")
			.MaximumLength(50)
				.WithMessage("Username must not exceed 50 characters.")
				.WithErrorCode("USERNAME_TOO_LONG")
			.Matches(@"^[a-zA-Z0-9_.-]+$")
				.WithMessage("Username may only contain letters, digits, underscores, hyphens, and dots.")
				.WithErrorCode("USERNAME_INVALID_CHARS");

		RuleFor(x => x.Email)
			.NotEmpty()
				.WithMessage("Email is required.")
				.WithErrorCode("EMAIL_REQUIRED")
			.EmailAddress()
				.WithMessage("A valid email address is required.")
				.WithErrorCode("EMAIL_INVALID");

		RuleFor(x => x.Password)
			.NotEmpty()
				.WithMessage("Password is required.")
				.WithErrorCode("PASSWORD_REQUIRED")
			.MinimumLength(8)
				.WithMessage("Password must be at least 8 characters.")
				.WithErrorCode("PASSWORD_TOO_SHORT")
			.MaximumLength(100)
				.WithMessage("Password must not exceed 100 characters.")
				.WithErrorCode("PASSWORD_TOO_LONG")
			.Matches(@"[A-Z]")
				.WithMessage("Password must contain at least one uppercase letter.")
				.WithErrorCode("PASSWORD_NO_UPPERCASE")
			.Matches(@"[a-z]")
				.WithMessage("Password must contain at least one lowercase letter.")
				.WithErrorCode("PASSWORD_NO_LOWERCASE")
			.Matches(@"[0-9]")
				.WithMessage("Password must contain at least one digit.")
				.WithErrorCode("PASSWORD_NO_DIGIT")
			.Matches(@"[^a-zA-Z0-9]")
				.WithMessage("Password must contain at least one special character (!@#$%^&* etc.).")
				.WithErrorCode("PASSWORD_NO_SPECIAL_CHAR");

		RuleFor(x => x.ConfirmPassword)
			.NotEmpty()
				.WithMessage("Confirm password is required.")
				.WithErrorCode("CONFIRM_PASSWORD_REQUIRED")
			.Equal(x => x.Password)
				.WithMessage("Passwords do not match.")
				.WithErrorCode("PASSWORDS_DO_NOT_MATCH");
	}
}
