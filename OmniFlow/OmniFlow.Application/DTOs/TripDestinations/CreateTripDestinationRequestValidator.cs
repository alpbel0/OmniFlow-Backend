using FluentValidation;

namespace OmniFlow.Application.DTOs.TripDestinations;

public class CreateTripDestinationRequestValidator : AbstractValidator<CreateTripDestinationRequest>
{
    public CreateTripDestinationRequestValidator()
    {
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.DepartureDate)
            .GreaterThanOrEqualTo(x => x.ArrivalDate)
            .WithMessage("Departure date must be greater than or equal to arrival date.");

        RuleFor(x => x.OrderIndex)
            .InclusiveBetween(1, 10)
            .WithMessage("OrderIndex must be between 1 and 10.");
    }
}
