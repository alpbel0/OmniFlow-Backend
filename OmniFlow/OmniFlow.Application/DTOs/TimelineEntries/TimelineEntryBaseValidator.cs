using FluentValidation;

namespace OmniFlow.Application.DTOs.TimelineEntries;

/// <summary>
/// Base validator for TimelineEntry DTOs containing shared validation rules.
/// Derived classes add their own specific rules (e.g., EntryType-based required fields).
/// </summary>
public abstract class TimelineEntryBaseValidator<T> : AbstractValidator<T>
    where T : class, ITimelineEntryValidationProperties
{
    protected TimelineEntryBaseValidator()
    {
        // Pricing
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Price must be >= 0.");

        RuleFor(x => x.CurrencyCode)
            .Matches(@"^[A-Z]{3}$")
            .When(x => !string.IsNullOrEmpty(x.CurrencyCode))
            .WithMessage("CurrencyCode must be exactly 3 uppercase letters.");

        // Coordinates
        RuleFor(x => x.CustomLatitude)
            .InclusiveBetween(-90.0, 90.0)
            .When(x => x.CustomLatitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.CustomLongitude)
            .InclusiveBetween(-180.0, 180.0)
            .When(x => x.CustomLongitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        // URL
        RuleFor(x => x.CustomPhotoUrl)
            .Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .When(x => !string.IsNullOrEmpty(x.CustomPhotoUrl))
            .WithMessage("CustomPhotoUrl must be a valid absolute URL.");

        // Flight timing
        RuleFor(x => x.FlightArrivalAt)
            .GreaterThan(x => x.FlightDepartureAt)
            .When(x => x.FlightDepartureAt.HasValue && x.FlightArrivalAt.HasValue)
            .WithMessage("FlightArrivalAt must be after FlightDepartureAt.");

        // Accommodation timing
        RuleFor(x => x.AccommodationCheckOut)
            .GreaterThan(x => x.AccommodationCheckIn)
            .When(x => x.AccommodationCheckIn.HasValue && x.AccommodationCheckOut.HasValue)
            .WithMessage("AccommodationCheckOut must be after AccommodationCheckIn.");

        // Duration
        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0)
            .When(x => x.DurationMinutes.HasValue)
            .WithMessage("DurationMinutes must be greater than 0.");
    }
}