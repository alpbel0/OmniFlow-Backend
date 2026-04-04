using FluentValidation;

namespace OmniFlow.Application.Features.Stops.Commands.UpdateStop;

public class UpdateStopCommandValidator : AbstractValidator<UpdateStopCommand>
{
    public UpdateStopCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("TripId is required.");

        RuleFor(x => x.StopId)
            .NotEmpty().WithMessage("StopId is required.");

        RuleFor(x => x.DayNumber)
            .GreaterThan(0).When(x => x.DayNumber.HasValue)
            .WithMessage("DayNumber must be greater than 0 when provided.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).When(x => x.DurationMinutes.HasValue)
            .WithMessage("DurationMinutes must be greater than 0 when provided.");

        RuleFor(x => x.ActivityPrice)
            .GreaterThanOrEqualTo(0).When(x => x.ActivityPrice.HasValue)
            .WithMessage("ActivityPrice must be greater than or equal to 0.");

        RuleFor(x => x.TransportPrice)
            .GreaterThanOrEqualTo(0).When(x => x.TransportPrice.HasValue)
            .WithMessage("TransportPrice must be greater than or equal to 0.");

        RuleFor(x => x.CurrencyCode)
            .Matches("^[A-Z]{3}$").When(x => !string.IsNullOrEmpty(x.CurrencyCode))
            .WithMessage("CurrencyCode must be 3 uppercase letters (e.g., USD, EUR).");

        // CHECK constraint: fallback_differs_from_place
        RuleFor(x => x)
            .Must(x => !x.FallbackPlaceId.HasValue || !x.PlaceId.HasValue || x.FallbackPlaceId != x.PlaceId)
            .WithMessage("FallbackPlaceId must be different from PlaceId.");
    }
}