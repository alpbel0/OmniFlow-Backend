using FluentValidation;

namespace OmniFlow.Application.Features.Trips.Commands.ForkTrip;

public class ForkTripCommandValidator : AbstractValidator<ForkTripCommand>
{
    public ForkTripCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");
    }
}