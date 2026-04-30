using FluentValidation;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class CreateTimelineEntryRequestValidator : TimelineEntryBaseValidator<CreateTimelineEntryRequest>
{
    public CreateTimelineEntryRequestValidator()
    {
        // Core
        RuleFor(x => x.TripId).NotEmpty().WithMessage("TripId is required.");
        RuleFor(x => x.DestinationId).NotEmpty().WithMessage("DestinationId is required.");
        RuleFor(x => x.DayNumber).GreaterThan(0).WithMessage("DayNumber must be greater than 0.");
        RuleFor(x => x.EntryType).IsInEnum().WithMessage("Invalid EntryType.");

        // Place → PlaceId zorunlu
        RuleFor(x => x.PlaceId)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.Place)
            .WithMessage("PlaceId is required when EntryType is Place.");

        // CustomFlight → Uçuş alanları zorunlu
        RuleFor(x => x.FlightFromAirport)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.CustomFlight)
            .WithMessage("FlightFromAirport is required.");
        RuleFor(x => x.FlightToAirport)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.CustomFlight)
            .WithMessage("FlightToAirport is required.");
        RuleFor(x => x.FlightDepartureAt)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomFlight)
            .WithMessage("FlightDepartureAt is required.");
        RuleFor(x => x.FlightArrivalAt)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomFlight)
            .WithMessage("FlightArrivalAt is required.");

        // CustomTransport → TransportType zorunlu
        RuleFor(x => x.TransportType)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomTransport)
            .WithMessage("TransportType is required.");

        // CustomAccommodation → Tarihler + isim zorunlu
        RuleFor(x => x.CustomName)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.CustomAccommodation)
            .WithMessage("CustomName (hotel/Airbnb name) is required.");
        RuleFor(x => x.AccommodationCheckIn)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomAccommodation)
            .WithMessage("AccommodationCheckIn is required.");
        RuleFor(x => x.AccommodationCheckOut)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomAccommodation)
            .WithMessage("AccommodationCheckOut is required.");

        // CustomEvent → İsim + zaman zorunlu
        RuleFor(x => x.CustomName)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.CustomEvent)
            .WithMessage("CustomName (event name) is required.");
        RuleFor(x => x.StartTime)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomEvent)
            .WithMessage("StartTime is required.");
    }
}