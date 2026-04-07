using FluentValidation;

namespace OmniFlow.Application.DTOs.Flights;

public class SelectFlightRequestValidator : AbstractValidator<SelectFlightRequest>
{
    public SelectFlightRequestValidator()
    {
        RuleFor(x => x.FlightId)
            .NotEmpty().WithMessage("FlightId is required.");
    }
}