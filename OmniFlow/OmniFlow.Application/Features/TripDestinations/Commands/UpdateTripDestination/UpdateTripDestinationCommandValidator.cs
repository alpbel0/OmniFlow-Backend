using FluentValidation;

namespace OmniFlow.Application.Features.TripDestinations.Commands.UpdateTripDestination;

public class UpdateTripDestinationCommandValidator : AbstractValidator<UpdateTripDestinationCommand>
{
    public UpdateTripDestinationCommandValidator()
    {
        RuleFor(x => x.DestinationId)
            .NotEmpty().WithMessage("Destination ID is required.");

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
