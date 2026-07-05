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

		RuleFor(command => command.LocationLatitude)
			.InclusiveBetween(-90, 90)
			.When(command => command.UpdateLocationCoordinates && command.LocationLatitude.HasValue);

		RuleFor(command => command.LocationLongitude)
			.InclusiveBetween(-180, 180)
			.When(command => command.UpdateLocationCoordinates && command.LocationLongitude.HasValue);

		RuleFor(command => command)
			.Must(command => command.LocationLatitude.HasValue == command.LocationLongitude.HasValue)
			.WithMessage("Latitude and longitude must be provided together.")
			.When(command => command.UpdateLocationCoordinates);

		RuleForEach(command => command.TravelStyles)
			.IsInEnum()
			.When(command => command.UpdateTravelStyles && command.TravelStyles != null);
	}
}
