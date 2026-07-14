using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Summary;

public static class TripSummaryCalculator
{
    public static CostCoverage CalculateCoverage(IEnumerable<PlaceVisitLog> visitLogs)
    {
        var logs = visitLogs.ToList();
        var visitsWithCost = logs.Count(x => x.ActualCost.HasValue);
        var missing = logs.Count - visitsWithCost;
        var pending = logs.Count(x => x.ConversionStatus == ConversionStatus.Pending);
        return new CostCoverage(
            visitsWithCost,
            missing,
            pending,
            missing == 0,
            pending == 0);
    }

    public static VisitCompletion CalculateCompletion(
        IEnumerable<TimelineEntry> timelineEntries,
        IEnumerable<PlaceVisitLog> visitLogs)
    {
        var visitableEntries = timelineEntries
            .Where(x => x.EntryType is TimelineEntryType.Place or TimelineEntryType.CustomEvent)
            .Select(x => x.Id)
            .ToHashSet();
        var logs = visitLogs.ToList();
        var visitedPlanned = logs
            .Where(x => x.TimelineEntryId.HasValue && visitableEntries.Contains(x.TimelineEntryId.Value))
            .Select(x => x.TimelineEntryId!.Value)
            .Distinct()
            .Count();
        var percentage = visitableEntries.Count == 0
            ? (decimal?)null
            : decimal.Round(visitedPlanned * 100m / visitableEntries.Count, 2);
        return new VisitCompletion(
            visitedPlanned,
            visitableEntries.Count,
            logs.Count(x => x.PlaceId.HasValue),
            percentage);
    }
}

public sealed record CostCoverage(
    int VisitsWithCostCount,
    int MissingCostCount,
    int PendingConversionCount,
    bool IsCostComplete,
    bool IsConversionComplete);

public sealed record VisitCompletion(
    int VisitedPlannedEntryCount,
    int PlannedVisitableEntryCount,
    int SpontaneousVisitCount,
    decimal? VisitCompletionPercentage);
