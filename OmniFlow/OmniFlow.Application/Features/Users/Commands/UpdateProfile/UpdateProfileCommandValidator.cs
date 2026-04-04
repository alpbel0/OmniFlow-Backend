using FluentValidation;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
	public UpdateProfileCommandValidator()
	{
		RuleFor(command => command.Bio)
			.MaximumLength(300)
			.When(command => command.Bio != null);
	}
}