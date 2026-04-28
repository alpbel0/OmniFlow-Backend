using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Services;

public class TimelineService : ITimelineService
{
    public int GetDailyCapacity(Tempo tempo) => tempo switch
    {
        Tempo.Slow => 3,
        Tempo.Moderate => 5,
        Tempo.Fast => 7,
        _ => 5
    };

    public (DateTime Start, DateTime End)? GetTimeRange(TimelineEntry entry, DateOnly destinationArrivalDate)
    {
        var baseDate = destinationArrivalDate.AddDays(entry.DayNumber - 1);

        return entry.EntryType switch
        {
            TimelineEntryType.Place when entry.StartTime.HasValue && entry.DurationMinutes.HasValue
                => (baseDate.ToDateTime(entry.StartTime.Value),
                    baseDate.ToDateTime(entry.StartTime.Value).AddMinutes(entry.DurationMinutes.Value)),

            TimelineEntryType.CustomEvent when entry.StartTime.HasValue && entry.DurationMinutes.HasValue
                => (baseDate.ToDateTime(entry.StartTime.Value),
                    baseDate.ToDateTime(entry.StartTime.Value).AddMinutes(entry.DurationMinutes.Value)),

            TimelineEntryType.CustomFlight when entry.FlightDepartureAt.HasValue && entry.FlightArrivalAt.HasValue
                => (entry.FlightDepartureAt.Value.AddMinutes(-(entry.BufferMinutes ?? 0)),
                    entry.FlightArrivalAt.Value),

            TimelineEntryType.CustomTransport when entry.StartTime.HasValue && entry.DurationMinutes.HasValue
                => (baseDate.ToDateTime(entry.StartTime.Value).AddMinutes(-(entry.BufferMinutes ?? 0)),
                    baseDate.ToDateTime(entry.StartTime.Value).AddMinutes(entry.DurationMinutes.Value)),

            TimelineEntryType.CustomAccommodation => null,

            _ => null
        };
    }

    public TimelineValidationResult CheckConflict(
        TimelineEntry newEntry,
        IEnumerable<TimelineEntry> existingEntries,
        DateOnly destinationArrivalDate)
    {
        var newRange = GetTimeRange(newEntry, destinationArrivalDate);
        if (newRange == null)
            return TimelineValidationResult.Valid();

        var (newStart, newEnd) = newRange.Value;

        foreach (var existing in existingEntries)
        {
            // Skip the entry itself (supports update scenarios)
            if (existing.Id == newEntry.Id)
                continue;

            var existingArrivalDate = existing.Destination?.ArrivalDate ?? destinationArrivalDate;
            var existingRange = GetTimeRange(existing, existingArrivalDate);
            if (existingRange == null)
                continue;

            // If new entry is NOT locked, only check against locked/time-bound entries.
            // If new entry IS locked, check against ALL time-bound entries.
            if (!newEntry.IsLocked && !existing.IsLocked)
                continue;

            var (exStart, exEnd) = existingRange.Value;

            // Overlap check: [newStart, newEnd) overlaps with [exStart, exEnd)
            if (newStart < exEnd && exStart < newEnd)
            {
                return TimelineValidationResult.Invalid(
                    $"Time conflict with existing '{existing.EntryType}' entry.",
                    "CONFLICT");
            }
        }

        return TimelineValidationResult.Valid();
    }

    public TimelineValidationResult ValidateNewEntry(
        TimelineEntry entry,
        IEnumerable<TimelineEntry> dayEntries,
        Tempo tempo,
        DateOnly destinationArrivalDate)
    {
        // 1. Check time conflicts
        var conflictResult = CheckConflict(entry, dayEntries, destinationArrivalDate);
        if (!conflictResult.IsValid)
            return conflictResult;

        // 2. Check daily capacity (only Place + CustomEvent count)
        var capacity = GetDailyCapacity(tempo);
        var activityCount = dayEntries.Count(e =>
            e.Id != entry.Id &&
            (e.EntryType == TimelineEntryType.Place || e.EntryType == TimelineEntryType.CustomEvent));

        if (entry.EntryType == TimelineEntryType.Place || entry.EntryType == TimelineEntryType.CustomEvent)
            activityCount++;

        if (activityCount > capacity)
        {
            return TimelineValidationResult.Invalid(
                $"Daily activity capacity exceeded. Maximum {capacity} activities allowed for {tempo} tempo.",
                "CAPACITY_EXCEEDED");
        }

        return TimelineValidationResult.Valid();
    }

    public double GetLexoRankBetween(double? previousIndex, double? nextIndex)
    {
        const double defaultStep = 500.0;

        if (previousIndex == null && nextIndex == null)
            return defaultStep;

        if (previousIndex == null)
            return nextIndex!.Value - defaultStep;

        if (nextIndex == null)
            return previousIndex!.Value + defaultStep;

        return (previousIndex!.Value + nextIndex!.Value) / 2.0;
    }
}
