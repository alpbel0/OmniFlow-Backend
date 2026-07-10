using FluentValidation;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class CreateTimelineEntryRequestValidator : TimelineEntryBaseValidator<CreateTimelineEntryRequest>
{
    public CreateTimelineEntryRequestValidator()
    {
        RuleFor(x => x.DestinationId).NotEmpty().WithMessage("DestinationId is required.");
        RuleFor(x => x.DayNumber).GreaterThan(0).WithMessage("DayNumber must be greater than 0.");
        RuleFor(x => x.EntryType).IsInEnum().WithMessage("Invalid EntryType.");

        RuleFor(x => x.PlanningSlotKey)
            .MaximumLength(160)
            .When(x => x.PlanningSlotKey != null)
            .WithMessage("PlanningSlotKey must be 160 characters or fewer.");

        RuleFor(x => x)
            .Must(x => !(x.ProviderFlightId.HasValue && x.ProviderHotelId.HasValue))
            .WithMessage("ProviderFlightId and ProviderHotelId cannot both be set.");

        RuleFor(x => x)
            .Must(x => !x.ProviderFlightId.HasValue || x.EntryType == TimelineEntryType.CustomFlight)
            .WithMessage("EntryType must be CustomFlight when ProviderFlightId is set.");

        RuleFor(x => x)
            .Must(x => !x.ProviderHotelId.HasValue || x.EntryType == TimelineEntryType.CustomAccommodation)
            .WithMessage("EntryType must be CustomAccommodation when ProviderHotelId is set.");

        RuleFor(x => x.PlaceId)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.Place)
            .WithMessage("PlaceId is required when EntryType is Place.");

        RuleFor(x => x.FlightFromAirport)
            .NotEmpty()
            .When(x => x.EntryType == TimelineEntryType.CustomFlight && !x.ProviderFlightId.HasValue)
            .WithMessage("FlightFromAirport is required.");

        RuleFor(x => x.FlightToAirport)
            .NotEmpty()
            .When(x => x.EntryType == TimelineEntryType.CustomFlight && !x.ProviderFlightId.HasValue)
            .WithMessage("FlightToAirport is required.");

        RuleFor(x => x.FlightDepartureAt)
            .NotNull()
            .When(x => x.EntryType == TimelineEntryType.CustomFlight && !x.ProviderFlightId.HasValue)
            .WithMessage("FlightDepartureAt is required.");

        RuleFor(x => x.FlightArrivalAt)
            .NotNull()
            .When(x => x.EntryType == TimelineEntryType.CustomFlight && !x.ProviderFlightId.HasValue)
            .WithMessage("FlightArrivalAt is required.");

        RuleFor(x => x.TransportType)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomTransport)
            .WithMessage("TransportType is required.");

        RuleFor(x => x.CustomName)
            .NotEmpty()
            .When(x => x.EntryType == TimelineEntryType.CustomAccommodation && !x.ProviderHotelId.HasValue)
            .WithMessage("CustomName (hotel/Airbnb name) is required.");

        RuleFor(x => x.AccommodationCheckIn)
            .NotNull()
            .When(x => x.EntryType == TimelineEntryType.CustomAccommodation && !x.ProviderHotelId.HasValue)
            .WithMessage("AccommodationCheckIn is required.");

        RuleFor(x => x.AccommodationCheckOut)
            .NotNull()
            .When(x => x.EntryType == TimelineEntryType.CustomAccommodation && !x.ProviderHotelId.HasValue)
            .WithMessage("AccommodationCheckOut is required.");

        RuleFor(x => x.CustomName)
            .NotEmpty().When(x => x.EntryType == TimelineEntryType.CustomEvent)
            .WithMessage("CustomName (event name) is required.");

        RuleFor(x => x.StartTime)
            .NotNull().When(x => x.EntryType == TimelineEntryType.CustomEvent)
            .WithMessage("StartTime is required.");
    }
}
