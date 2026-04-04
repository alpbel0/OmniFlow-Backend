using FluentValidation;

namespace OmniFlow.Application.Features.Flights.Commands.SelectFlight;

public class SelectFlightCommandValidator : AbstractValidator<SelectFlightCommand>
{
    public SelectFlightCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");

        RuleFor(x => x.FlightId)
            .NotEmpty().WithMessage("FlightId is required.");
    }
}