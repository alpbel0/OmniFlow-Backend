using FluentValidation;

namespace OmniFlow.Application.Features.Users.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
	public UpdateProfileCommandValidator()
	{
		RuleFor(command => command.Bio)
			.MaximumLength(300)
			.When(command => command.UpdateBio && command.Bio != null);

		RuleFor(command => command.Location)
			.MaximumLength(120)
			.When(command => command.UpdateLocation && command.Location != null);

		RuleForEach(command => command.TravelStyles)
			.IsInEnum()
			.When(command => command.UpdateTravelStyles && command.TravelStyles != null);
	}
}
