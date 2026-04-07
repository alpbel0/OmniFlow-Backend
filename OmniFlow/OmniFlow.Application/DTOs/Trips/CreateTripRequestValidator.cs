using FluentValidation;

namespace OmniFlow.Application.DTOs.Trips;

public class CreateTripRequestValidator : AbstractValidator<CreateTripRequest>
{
	public CreateTripRequestValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

		RuleFor(x => x.City)
			.NotEmpty().WithMessage("City is required.");

		RuleFor(x => x.Country)
			.NotEmpty().WithMessage("Country is required.");

		RuleFor(x => x.EndDate)
			.GreaterThanOrEqualTo(x => x.StartDate)
			.WithMessage("End date must be greater than or equal to start date.");

		RuleFor(x => x.PersonCount)
			.GreaterThan(0).WithMessage("Person count must be greater than 0.");

		RuleFor(x => x.UserBudget)
			.GreaterThanOrEqualTo(0).When(x => x.UserBudget.HasValue)
			.WithMessage("User budget must be greater than or equal to 0.");
	}
}