using FluentValidation;
using OmniFlow.Application.DTOs.TripDestinations;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTripWizard;

public class CreateTripWizardCommandValidator : AbstractValidator<CreateTripWizardCommand>
{
    public CreateTripWizardCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must not exceed 100 characters.");

        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Origin city is required.");

        RuleFor(x => x.OriginCountry)
            .NotEmpty().WithMessage("Origin country is required.");

        RuleFor(x => x.PersonCount)
            .GreaterThan(0).WithMessage("Person count must be greater than 0.");

        RuleFor(x => x.TravelStyles)
            .Must(styles => styles == null || styles.Count <= 3)
            .WithMessage("At most 3 travel styles can be selected.");

        RuleFor(x => x.ManualBudget)
            .GreaterThanOrEqualTo(0).When(x => x.ManualBudget.HasValue)
            .WithMessage("Manual budget must be greater than or equal to 0.");

        // Destinations: must have at least 1, at most 10
        RuleFor(x => x.Destinations)
            .NotNull().WithMessage("At least one destination is required.")
            .Must(d => d.Count >= 1).WithMessage("At least one destination is required.")
            .Must(d => d.Count <= 10).WithMessage("At most 10 destinations can be selected.");

        // Each destination validation
        RuleForEach(x => x.Destinations).SetValidator(new CreateTripDestinationRequestValidator());

        // Sequential date validation: Departure[i] <= Arrival[i+1]
        RuleFor(x => x.Destinations)
            .Must(HasValidSequentialDates)
            .WithMessage("Each destination's departure date must be on or before the next destination's arrival date.")
            .When(x => x.Destinations != null && x.Destinations.Count > 1);
    }

    private static bool HasValidSequentialDates(List<CreateTripDestinationRequest> destinations)
    {
        var sorted = destinations
            .OrderBy(d => d.OrderIndex)
            .ToList();

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            if (sorted[i].DepartureDate > sorted[i + 1].ArrivalDate)
                return false;
        }

        return true;
    }
}
