using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Services;

public sealed class TripTemporalService(IDateTimeService dateTimeService) : ITripTemporalService
{
    public TripExecutionStateResult GetExecutionState(Trip trip)
    {
        var destinations = trip.Destinations
            .Where(destination => destination.DeletedAt is null)
            .OrderBy(destination => destination.OrderIndex)
            .ToList();

        if (destinations.Count == 0 || destinations.Any(destination => string.IsNullOrWhiteSpace(destination.Timezone)))
            return new TripExecutionStateResult(null, false);

        var firstLocalDate = GetLocalDate(dateTimeService.NowUtc, destinations[0].Timezone!);
        if (firstLocalDate < trip.StartDate)
            return new TripExecutionStateResult(TripExecutionState.Upcoming, true);

        var lastLocalDate = GetLocalDate(dateTimeService.NowUtc, destinations[^1].Timezone!);
        var state = lastLocalDate > trip.EndDate
            ? TripExecutionState.Completed
            : TripExecutionState.Active;
        return new TripExecutionStateResult(state, true);
    }

    public DateOnly GetLocalDate(DateTime utcInstant, string timezone)
    {
        if (utcInstant.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The instant must be UTC.", nameof(utcInstant));

        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcInstant, timeZoneInfo));
    }
}
