using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface ITimelineService
{
    /// <summary>
    /// Returns the max number of activities (Place + CustomEvent) allowed per day based on tempo.
    /// </summary>
    int GetDailyCapacity(Tempo tempo);

    /// <summary>
    /// Calculates the absolute DateTime range for a timeline entry within the destination's context.
    /// Formula: Destination.ArrivalDate + (DayNumber - 1) days + StartTime.
    /// Returns null for entries without temporal bounds (e.g. CustomAccommodation) or when required time fields are missing.
    /// </summary>
    (DateTime Start, DateTime End)? GetTimeRange(TimelineEntry entry, DateOnly destinationArrivalDate);

    /// <summary>
    /// Checks whether the new entry conflicts with existing locked/time-bound entries.
    /// </summary>
    TimelineValidationResult CheckConflict(
        TimelineEntry newEntry,
        IEnumerable<TimelineEntry> existingEntries,
        DateOnly destinationArrivalDate);

    /// <summary>
    /// Validates a new entry against capacity and time-conflict rules for the day.
    /// </summary>
    TimelineValidationResult ValidateNewEntry(
        TimelineEntry entry,
        IEnumerable<TimelineEntry> dayEntries,
        Tempo tempo,
        DateOnly destinationArrivalDate);

    /// <summary>
    /// Calculates a LexoRank value between two existing order indices.
    /// </summary>
    double GetLexoRankBetween(double? previousIndex, double? nextIndex);
}
