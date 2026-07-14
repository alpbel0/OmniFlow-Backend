using Moq;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class TripTemporalServiceTests
{
    [Fact]
    public void GetExecutionState_WhenTripHasMissingTimezone_ReturnsUnavailable()
    {
        var service = CreateService(new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc));
        var trip = CreateTrip(null, "Europe/Paris");

        var result = service.GetExecutionState(trip);

        result.IsTimezoneComplete.Should().BeFalse();
        result.State.Should().BeNull();
    }

    [Fact]
    public void GetExecutionState_UsesFirstDestinationLocalDateForUpcomingBoundary()
    {
        var service = CreateService(new DateTime(2026, 7, 12, 21, 30, 0, DateTimeKind.Utc));
        var trip = CreateTrip("Europe/Istanbul", "Europe/Paris");

        var result = service.GetExecutionState(trip);

        result.State.Should().Be(TripExecutionState.Active);
        result.IsTimezoneComplete.Should().BeTrue();
    }

    [Fact]
    public void GetExecutionState_UsesLastDestinationLocalDateForCompletedBoundary()
    {
        var service = CreateService(new DateTime(2026, 7, 15, 22, 30, 0, DateTimeKind.Utc));
        var trip = CreateTrip("Europe/Istanbul", "Europe/Paris");

        var result = service.GetExecutionState(trip);

        result.State.Should().Be(TripExecutionState.Completed);
    }

    private static TripTemporalService CreateService(DateTime nowUtc)
    {
        var clock = new Mock<IDateTimeService>();
        clock.SetupGet(x => x.NowUtc).Returns(nowUtc);
        return new TripTemporalService(clock.Object);
    }

    private static Trip CreateTrip(string? firstTimezone, string? lastTimezone)
    {
        var trip = new Trip { Id = Guid.NewGuid() };
        trip.Destinations.Add(CreateDestination(trip.Id, 1, new DateOnly(2026, 7, 13), new DateOnly(2026, 7, 14), firstTimezone));
        trip.Destinations.Add(CreateDestination(trip.Id, 2, new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 15), lastTimezone));
        trip.RecalculateFromDestinations();
        return trip;
    }

    private static TripDestination CreateDestination(
        Guid tripId,
        int order,
        DateOnly arrival,
        DateOnly departure,
        string? timezone)
    {
        var destination = new TripDestination(arrival, departure, "City", "Country", order)
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            Timezone = timezone
        };
        return destination;
    }
}
