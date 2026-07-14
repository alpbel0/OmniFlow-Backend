using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Summary;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripSummary;

public sealed record GetTripSummaryQuery(Guid TripId) : IRequest<TripSummaryResponse>;

public sealed class GetTripSummaryQueryHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService,
    ITripTemporalService temporalService,
    IExchangeRateService exchangeRateService)
    : IRequestHandler<GetTripSummaryQuery, TripSummaryResponse>
{
    public async Task<TripSummaryResponse> Handle(GetTripSummaryQuery request, CancellationToken cancellationToken)
    {
        var trip = await context.Trips.Include(x => x.Destinations)
            .FirstOrDefaultAsync(x => x.Id == request.TripId, cancellationToken)
            ?? throw new EntityNotFoundException("Trip", request.TripId);
        if (trip.OwnerId != Guid.Parse(authenticatedUserService.UserId))
            throw new ForbiddenException("You are not authorized to view this trip summary.");
        var execution = temporalService.GetExecutionState(trip);
        if (!execution.IsTimezoneComplete)
            throw new ApiException("Trip timezone information is incomplete.", 409, "TIMEZONE_UNAVAILABLE");
        if (execution.State == TripExecutionState.Upcoming)
            throw new ApiException("The trip has not started.", 409, "TRIP_NOT_STARTED");

        var logs = await context.PlaceVisitLogs.Include(x => x.Place)
            .Where(x => x.TripId == trip.Id).ToListAsync(cancellationToken);
        var visitedTimelineEntryIds = logs.Where(x => x.TimelineEntryId.HasValue)
            .Select(x => x.TimelineEntryId!.Value)
            .ToArray();
        var entries = await context.TimelineEntries.IgnoreQueryFilters().Include(x => x.Place)
            .Where(x => x.TripId == trip.Id &&
                        (x.DeletedAt == null || visitedTimelineEntryIds.Contains(x.Id)))
            .ToListAsync(cancellationToken);
        var completion = TripSummaryCalculator.CalculateCompletion(entries, logs);
        var coverage = TripSummaryCalculator.CalculateCoverage(logs);
        var plannedCost = await CalculatePlannedCostAsync(trip, entries, cancellationToken);
        var canonicalPlaces = ResolveCanonicalPlaces(logs, entries);
        var markers = BuildMarkers(logs, entries);

        return new TripSummaryResponse
        {
            TripId = trip.Id,
            ExecutionState = execution.State!.Value,
            BaseCurrencyCode = trip.BaseCurrencyCode,
            TotalVisitCount = logs.Count,
            UniquePlaceCount = canonicalPlaces.Values.Where(x => x.PlaceId.HasValue).Select(x => x.PlaceId).Distinct().Count(),
            VisitedCustomEventCount = canonicalPlaces.Values.Count(x => x.IsCustomEvent),
            SpontaneousVisitCount = completion.SpontaneousVisitCount,
            VisitedPlannedEntryCount = completion.VisitedPlannedEntryCount,
            PlannedVisitableEntryCount = completion.PlannedVisitableEntryCount,
            VisitCompletionPercentage = completion.VisitCompletionPercentage,
            PlannedVisitCost = plannedCost.Amount,
            ActualVisitCost = logs.Where(x => x.ConversionStatus == ConversionStatus.Completed).Sum(x => x.ConvertedActualCost ?? 0m),
            VisitsWithCostCount = coverage.VisitsWithCostCount,
            MissingCostCount = coverage.MissingCostCount,
            PendingConversionCount = coverage.PendingConversionCount,
            PendingPlannedConversionCount = plannedCost.PendingCount,
            IsCostComplete = coverage.IsCostComplete,
            IsConversionComplete = coverage.IsConversionComplete,
            UnmappedVisitCount = logs.Count - markers.Count,
            OriginalCurrencyBreakdown = BuildCurrencyBreakdown(logs),
            Favorites = BuildFavorites(logs, canonicalPlaces),
            VisitMarkers = markers,
            Destinations = trip.Destinations.OrderBy(x => x.OrderIndex).Select(destination =>
                new TripSummaryDestinationResponse(
                    destination.Id, destination.City, destination.Country, destination.OrderIndex,
                    logs.Count(log => log.TripDestinationId == destination.Id))).ToList()
        };
    }

    private async Task<(decimal Amount, int PendingCount)> CalculatePlannedCostAsync(
        Trip trip,
        IReadOnlyList<TimelineEntry> entries,
        CancellationToken cancellationToken)
    {
        decimal total = 0;
        var pending = 0;
        foreach (var entry in entries.Where(x =>
                     (x.EntryType is TimelineEntryType.Place or TimelineEntryType.CustomEvent) && x.Price > 0))
        {
            var destination = trip.Destinations.First(x => x.Id == entry.DestinationId);
            var localDate = destination.ArrivalDate.AddDays(Math.Max(0, entry.DayNumber - 1));
            try
            {
                var rate = await exchangeRateService.GetRateAsync(entry.CurrencyCode, trip.BaseCurrencyCode, localDate, cancellationToken);
                total += entry.Price * rate.Rate;
            }
            catch (ApiException)
            {
                pending++;
            }
        }
        return (decimal.Round(total, 2, MidpointRounding.AwayFromZero), pending);
    }

    private static Dictionary<Guid, CanonicalVisit> ResolveCanonicalPlaces(
        IReadOnlyList<PlaceVisitLog> logs,
        IReadOnlyList<TimelineEntry> entries)
    {
        var entriesById = entries.ToDictionary(x => x.Id);
        return logs.ToDictionary(log => log.Id, log =>
        {
            if (log.PlaceId.HasValue)
                return new CanonicalVisit(log.PlaceId, false, log.Place?.Name ?? "Place");
            if (!entriesById.TryGetValue(log.TimelineEntryId!.Value, out var entry))
                return new CanonicalVisit(null, true, "Deleted timeline entry");
            return entry.EntryType == TimelineEntryType.Place
                ? new CanonicalVisit(entry.PlaceId, false, entry.Place?.Name ?? "Place")
                : new CanonicalVisit(null, true, entry.CustomName ?? "Custom event");
        });
    }

    private static List<TripFavoriteResponse> BuildFavorites(
        IReadOnlyList<PlaceVisitLog> logs,
        IReadOnlyDictionary<Guid, CanonicalVisit> canonical)
    {
        return logs.Where(x => x.Rating.HasValue)
            .GroupBy(x => canonical[x.Id].PlaceId ?? x.TimelineEntryId!.Value)
            .Select(group => new TripFavoriteResponse(
                group.Key,
                canonical[group.First().Id].IsCustomEvent ? "customEvent" : "place",
                canonical[group.First().Id].Name,
                decimal.Round((decimal)group.Average(x => x.Rating!.Value), 1),
                group.Count(x => x.Rating.HasValue),
                logs.Count(x => (canonical[x.Id].PlaceId ?? x.TimelineEntryId) == group.Key),
                group.Max(x => x.VisitedAt)))
            .OrderByDescending(x => x.AveragePersonalRating)
            .ThenByDescending(x => x.VisitCount)
            .ThenByDescending(x => x.LastVisitedAt)
            .ToList();
    }

    private static IReadOnlyList<CurrencyAmountBreakdown> BuildCurrencyBreakdown(IEnumerable<PlaceVisitLog> logs) =>
        logs.Where(x => x.ActualCost.HasValue).GroupBy(x => x.CurrencyCode)
            .Select(x => new CurrencyAmountBreakdown(x.Key, x.Sum(log => log.ActualCost!.Value), x.Count()))
            .OrderBy(x => x.CurrencyCode).ToList();

    private static List<TripVisitMarkerResponse> BuildMarkers(
        IEnumerable<PlaceVisitLog> logs,
        IReadOnlyList<TimelineEntry> entries)
    {
        var entriesById = entries.ToDictionary(x => x.Id);
        var result = new List<TripVisitMarkerResponse>();
        foreach (var log in logs)
        {
            TimelineEntry? entry = null;
            if (log.TimelineEntryId.HasValue)
                entriesById.TryGetValue(log.TimelineEntryId.Value, out entry);
            var latitude = entry?.Place?.Latitude ?? entry?.CustomLatitude ?? log.Place?.Latitude;
            var longitude = entry?.Place?.Longitude ?? entry?.CustomLongitude ?? log.Place?.Longitude;
            if (latitude.HasValue && longitude.HasValue)
                result.Add(new TripVisitMarkerResponse(log.Id, entry is null ? "spontaneous" : "plannedVisited", latitude.Value, longitude.Value, log.VisitedAt));
        }
        return result;
    }

    private sealed record CanonicalVisit(Guid? PlaceId, bool IsCustomEvent, string Name);
}
