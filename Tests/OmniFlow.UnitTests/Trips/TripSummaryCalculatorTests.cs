using OmniFlow.Application.Features.Trips.Summary;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class TripSummaryCalculatorTests
{
    [Fact]
    public void CalculateCoverage_SeparatesMissingZeroAndPendingCosts()
    {
        var logs = new[]
        {
            CreateLog(null, "USD", "USD"),
            CreateLog(0m, "USD", "USD"),
            CreateLog(10m, "TRY", "USD")
        };

        var result = TripSummaryCalculator.CalculateCoverage(logs);

        result.VisitsWithCostCount.Should().Be(2);
        result.MissingCostCount.Should().Be(1);
        result.PendingConversionCount.Should().Be(1);
        result.IsCostComplete.Should().BeFalse();
        result.IsConversionComplete.Should().BeFalse();
    }

    [Fact]
    public void CalculateCompletion_ExcludesSpontaneousVisitsAndNonVisitableEntries()
    {
        var plannedPlace = TimelineEntry.CreatePlaceEntry(Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid());
        var flight = TimelineEntry.CreateCustomFlightEntry(
            plannedPlace.TripId, plannedPlace.DestinationId, 1, 2,
            "IST", "CDG", DateTime.UtcNow, DateTime.UtcNow.AddHours(3));
        var plannedLog = CreateLog(null, "USD", "USD", plannedPlace.Id, null);
        var spontaneousLog = CreateLog(null, "USD", "USD", null, Guid.NewGuid());

        var result = TripSummaryCalculator.CalculateCompletion([plannedPlace, flight], [plannedLog, spontaneousLog]);

        result.PlannedVisitableEntryCount.Should().Be(1);
        result.VisitedPlannedEntryCount.Should().Be(1);
        result.SpontaneousVisitCount.Should().Be(1);
        result.VisitCompletionPercentage.Should().Be(100m);
    }

    private static PlaceVisitLog CreateLog(
        decimal? cost,
        string currency,
        string baseCurrency,
        Guid? timelineEntryId = null,
        Guid? placeId = null)
    {
        timelineEntryId ??= placeId.HasValue ? null : Guid.NewGuid();
        return PlaceVisitLog.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), timelineEntryId, placeId,
            DateTime.UtcNow, cost, currency, null, null, baseCurrency);
    }
}
