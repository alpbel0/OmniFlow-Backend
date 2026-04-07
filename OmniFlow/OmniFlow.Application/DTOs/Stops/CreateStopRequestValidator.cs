using FluentValidation;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Stops;

public class CreateStopRequestValidator : AbstractValidator<CreateStopRequest>
{
    public CreateStopRequestValidator()
    {
        RuleFor(x => x.DayNumber)
            .GreaterThan(0).WithMessage("DayNumber must be greater than 0.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).When(x => x.DurationMinutes.HasValue)
            .WithMessage("DurationMinutes must be greater than 0 when provided.");

        RuleFor(x => x.ActivityPrice)
            .GreaterThanOrEqualTo(0).WithMessage("ActivityPrice must be greater than or equal to 0.");

        RuleFor(x => x.TransportPrice)
            .GreaterThanOrEqualTo(0).WithMessage("TransportPrice must be greater than or equal to 0.");

        RuleFor(x => x.CurrencyCode)
            .Matches("^[A-Z]{3}$").When(x => !string.IsNullOrEmpty(x.CurrencyCode))
            .WithMessage("CurrencyCode must be 3 uppercase letters (e.g., USD, EUR).");

        // CHECK constraint: place_or_custom_name
        RuleFor(x => x)
            .Must(x => x.PlaceId.HasValue || !string.IsNullOrWhiteSpace(x.CustomName))
            .WithMessage("Either PlaceId or CustomName must be provided.");

        // CHECK constraint: custom_place_requires_category
        RuleFor(x => x.CustomCategory)
            .NotNull().When(x => !string.IsNullOrWhiteSpace(x.CustomName))
            .WithMessage("CustomCategory is required when CustomName is provided.");

        // CHECK constraint: time_lock_requires_arrival
        RuleFor(x => x.ArrivalTime)
            .NotNull().When(x => x.IsTimeLocked)
            .WithMessage("ArrivalTime is required when IsTimeLocked is true.");

        // CHECK constraint: fallback_differs_from_place
        RuleFor(x => x)
            .Must(x => !x.FallbackPlaceId.HasValue || !x.PlaceId.HasValue || x.FallbackPlaceId != x.PlaceId)
            .WithMessage("FallbackPlaceId must be different from PlaceId.");
    }
}