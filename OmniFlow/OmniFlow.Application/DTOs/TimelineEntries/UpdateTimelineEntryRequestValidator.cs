using FluentValidation;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.TimelineEntries;

public class UpdateTimelineEntryRequestValidator : TimelineEntryBaseValidator<UpdateTimelineEntryRequest>
{
    public UpdateTimelineEntryRequestValidator()
    {
        // Core
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        RuleFor(x => x.DestinationId).NotEmpty().WithMessage("DestinationId is required.");
        RuleFor(x => x.DayNumber).GreaterThan(0).WithMessage("DayNumber must be greater than 0.");

        // Flight alanları (eğer herhangi biri doldurulmuşsa diğerleri de zorunlu)
        RuleFor(x => x.FlightFromAirport)
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.FlightToAirport) || x.FlightDepartureAt.HasValue)
            .WithMessage("FlightFromAirport is required when other flight fields are provided.");
        RuleFor(x => x.FlightToAirport)
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.FlightFromAirport) || x.FlightDepartureAt.HasValue)
            .WithMessage("FlightToAirport is required when other flight fields are provided.");
        RuleFor(x => x.FlightDepartureAt)
            .NotNull().When(x => !string.IsNullOrEmpty(x.FlightFromAirport) || x.FlightArrivalAt.HasValue)
            .WithMessage("FlightDepartureAt is required when other flight fields are provided.");
        RuleFor(x => x.FlightArrivalAt)
            .NotNull().When(x => x.FlightDepartureAt.HasValue)
            .WithMessage("FlightArrivalAt is required when FlightDepartureAt is provided.");

        // Transport
        RuleFor(x => x.TransportType)
            .IsInEnum().When(x => x.TransportType.HasValue)
            .WithMessage("Invalid TransportType value.");
    }
}