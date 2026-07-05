using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Trips.Queries.GetMyTrips;

public static class TripCompletionCalculator
{
    public static int Calculate(
        Trip trip,
        IReadOnlyCollection<TripDestination>? destinations,
        IReadOnlyCollection<TimelineEntry>? timelineEntries)
    {
        var activeDestinations = destinations?.ToList() ?? [];
        var activeEntries = timelineEntries?.ToList() ?? [];

        var score =
            CalculateBasicInfoScore(trip) +
            CalculateTravelInfoScore(trip) +
            CalculateDestinationScore(activeDestinations) +
            CalculateTimelineScore(activeDestinations, activeEntries) +
            CalculateBudgetScore(trip);

        return Math.Clamp(score, 0, 100);
    }

    private static int CalculateBasicInfoScore(Trip trip)
    {
        var score = 0;
        score += HasText(trip.Title) ? 8 : 0;
        score += HasText(trip.Description) ? 6 : 0;
        score += HasText(trip.CoverPhotoUrl) ? 6 : 0;
        return score;
    }

    private static int CalculateTravelInfoScore(Trip trip)
    {
        var score = 0;
        score += HasText(trip.Origin) && HasText(trip.OriginCountry) ? 8 : 0;
        score += HasValidDateRange(trip.StartDate, trip.EndDate) ? 7 : 0;
        score += trip.PersonCount > 0 ? 4 : 0;
        score += trip.TravelStyles.Count > 0 ? 6 : 0;
        return score;
    }

    private static int CalculateDestinationScore(IReadOnlyCollection<TripDestination> destinations)
    {
        if (destinations.Count == 0)
            return 0;

        var score = 15;
        score += HasSequentialDestinationDates(destinations) ? 5 : 0;
        score += HasValidDestinationOrder(destinations) ? 5 : 0;
        return score;
    }

    private static int CalculateTimelineScore(
        IReadOnlyCollection<TripDestination> destinations,
        IReadOnlyCollection<TimelineEntry> entries)
    {
        if (entries.Count == 0)
            return 0;

        var score = 10;
        var coveredDestinationIds = entries.Select(e => e.DestinationId).ToHashSet();
        score += destinations.Count > 0 && destinations.All(d => coveredDestinationIds.Contains(d.Id)) ? 7 : 0;
        score += entries.All(e => e.DayNumber > 0 || e.StartTime.HasValue) ? 3 : 0;
        return score;
    }

    private static int CalculateBudgetScore(Trip trip)
    {
        var score = Enum.IsDefined(trip.BudgetTier) ? 5 : 0;
        score += trip.EstimatedCost.HasValue || trip.ManualBudget.HasValue ? 5 : 0;
        return score;
    }

    private static bool HasSequentialDestinationDates(IReadOnlyCollection<TripDestination> destinations)
    {
        var orderedDestinations = destinations.OrderBy(d => d.OrderIndex).ToList();

        if (orderedDestinations.Any(d => !HasValidDateRange(d.ArrivalDate, d.DepartureDate)))
            return false;

        return orderedDestinations
            .Zip(orderedDestinations.Skip(1), (current, next) => next.ArrivalDate >= current.DepartureDate)
            .All(isSequential => isSequential);
    }

    private static bool HasValidDestinationOrder(IReadOnlyCollection<TripDestination> destinations)
    {
        var orderedIndexes = destinations.Select(d => d.OrderIndex).Order().ToList();
        return orderedIndexes.SequenceEqual(Enumerable.Range(1, orderedIndexes.Count));
    }

    private static bool HasValidDateRange(DateOnly startDate, DateOnly endDate)
    {
        return startDate != default && endDate != default && endDate >= startDate;
    }

    private static bool HasText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
