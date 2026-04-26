using FluentValidation;

namespace OmniFlow.Application.DTOs.Trips;

public class UpdateTripRequestValidator : AbstractValidator<UpdateTripRequest>
{
    public UpdateTripRequestValidator()
    {
        // TripId comes from route parameter, not request body - skip validation

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Origin city is required.");

        RuleFor(x => x.OriginCountry)
            .NotEmpty().WithMessage("Origin country is required.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.PersonCount)
            .GreaterThan(0).WithMessage("Person count must be greater than 0.");

        RuleFor(x => x.TravelStyles)
            .Must(styles => styles == null || styles.Count <= 3)
            .WithMessage("At most 3 travel styles can be selected.");

        RuleFor(x => x.ManualBudget)
            .GreaterThanOrEqualTo(0).When(x => x.ManualBudget.HasValue)
            .WithMessage("Manual budget must be greater than or equal to 0.");
    }
}
