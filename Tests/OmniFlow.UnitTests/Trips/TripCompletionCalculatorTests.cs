using OmniFlow.Application.Features.Trips.Queries.GetMyTrips;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class TripCompletionCalculatorTests
{
    [Fact]
    public void Calculate_FullReadyTrip_Returns100()
    {
        var trip = CreateBaseTrip();
        trip.Description = "A complete itinerary";
        trip.CoverPhotoUrl = "https://example.com/cover.jpg";
        trip.EstimatedCost = 1200;

        var destination = CreateDestination(trip.Id, 1, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13));
        trip.Destinations.Add(destination);
        trip.RecalculateFromDestinations();

        var entry = TimelineEntry.CreateCustomEventEntry(
            trip.Id,
            destination.Id,
            1,
            1000,
            "Museum",
            new TimeOnly(10, 0),
            90);
        var secondEntry = TimelineEntry.CreateCustomEventEntry(
            trip.Id,
            destination.Id,
            1,
            1001,
            "Dinner",
            new TimeOnly(19, 0),
            60);

        var result = TripCompletionCalculator.Calculate(trip, [destination], [entry, secondEntry]);

        result.Should().Be(100);
    }

    [Fact]
    public void Calculate_PartialTrip_ReturnsExpectedScore()
    {
        var trip = CreateBaseTrip();

        var result = TripCompletionCalculator.Calculate(trip, [], []);

        result.Should().Be(19);
    }

    [Fact]
    public void Calculate_NullRelationshipCollections_DoesNotThrowAndRelationshipScoresAreZero()
    {
        var trip = new Trip
        {
            PersonCount = 0,
            BudgetTier = (BudgetTier)99
        };

        var result = TripCompletionCalculator.Calculate(trip, null, null);

        result.Should().Be(0);
    }

    [Fact]
    public void Calculate_SingleDestination_GivesDestinationOrderScore()
    {
        var trip = CreateTripWithoutBaseScore();
        var destination = CreateDestination(trip.Id, 1, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13));

        var result = TripCompletionCalculator.Calculate(trip, [destination], []);

        result.Should().Be(20);
    }

    [Fact]
    public void Calculate_MultipleDestinationsWithBrokenOrder_DoesNotGiveOrderScore()
    {
        var trip = CreateTripWithoutBaseScore();
        var firstDestination = CreateDestination(trip.Id, 1, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13));
        var secondDestination = CreateDestination(trip.Id, 3, new DateOnly(2026, 8, 13), new DateOnly(2026, 8, 16));

        var result = TripCompletionCalculator.Calculate(trip, [firstDestination, secondDestination], []);

        result.Should().Be(15);
    }

    private static Trip CreateBaseTrip()
    {
        return new Trip
        {
            Id = Guid.NewGuid(),
            Title = "Paris Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = [TravelStyle.Cultural]
        };
    }

    private static Trip CreateTripWithoutBaseScore()
    {
        return new Trip
        {
            Id = Guid.NewGuid(),
            PersonCount = 0,
            BudgetTier = (BudgetTier)99
        };
    }

    private static TripDestination CreateDestination(
        Guid tripId,
        int orderIndex,
        DateOnly arrivalDate,
        DateOnly departureDate)
    {
        return new TripDestination(arrivalDate, departureDate, "Paris", "France", orderIndex)
        {
            Id = Guid.NewGuid(),
            TripId = tripId
        };
    }
}
