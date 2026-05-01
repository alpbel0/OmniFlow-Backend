using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.ForkTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Trips;

public class ForkTripCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly Mock<IKarmaService> _karmaServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ForkTripCommandHandler _handler;

    public ForkTripCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _karmaServiceMock = new Mock<IKarmaService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _handler = new ForkTripCommandHandler(
            _tripRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _karmaServiceMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsForkedTripId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var originalTrip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published,
            Title = "Test Trip",
            Description = "Test Description",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure },
            ForkCount = 5,
            UpvoteCount = 10,
            ViewCount = 100,
            PopularityScore = 50.5m,
            Tags = new List<string> { "adventure", "nature" },
            Flights = new List<Flight>(),
            Hotels = new List<Hotel>()
        };

        _tripRepositoryMock
            .Setup(x => x.GetWithAllRelatedDataAsync(tripId))
            .ReturnsAsync(originalTrip);

        // Mock DbSet for Trips
        var trips = new List<Trip>();
        var mockTripsSet = MockDbSetHelper.CreateAsyncMockDbSet(trips);
        mockTripsSet.Setup(x => x.AddAsync(It.IsAny<Trip>(), default))
            .Callback<Trip, CancellationToken>((t, _) => trips.Add(t));
        _contextMock.Setup(x => x.Trips).Returns(mockTripsSet.Object);

        // Mock DbSet for TripDestinations
        var destinations = new List<TripDestination>();
        var mockDestSet = MockDbSetHelper.CreateAsyncMockDbSet(destinations);
        mockDestSet.Setup(x => x.AddAsync(It.IsAny<TripDestination>(), default))
            .Callback<TripDestination, CancellationToken>((d, _) => destinations.Add(d));
        _contextMock.Setup(x => x.TripDestinations).Returns(mockDestSet.Object);

        // Mock DbSet for TimelineEntries
        var timelineEntries = new List<TimelineEntry>();
        var mockEntrySet = MockDbSetHelper.CreateAsyncMockDbSet(timelineEntries);
        mockEntrySet.Setup(x => x.AddAsync(It.IsAny<TimelineEntry>(), default))
            .Callback<TimelineEntry, CancellationToken>((e, _) => timelineEntries.Add(e));
        _contextMock.Setup(x => x.TimelineEntries).Returns(mockEntrySet.Object);

        // Mock DbSet for Flights
        var flights = new List<Flight>();
        var mockFlightsSet = MockDbSetHelper.CreateAsyncMockDbSet(flights);
        mockFlightsSet.Setup(x => x.Add(It.IsAny<Flight>()))
            .Callback<Flight>(f => flights.Add(f));
        _contextMock.Setup(x => x.Flights).Returns(mockFlightsSet.Object);

        // Mock DbSet for Hotels
        var hotels = new List<Hotel>();
        var mockHotelsSet = MockDbSetHelper.CreateAsyncMockDbSet(hotels);
        mockHotelsSet.Setup(x => x.Add(It.IsAny<Hotel>()))
            .Callback<Hotel>(h => hotels.Add(h));
        _contextMock.Setup(x => x.Hotels).Returns(mockHotelsSet.Object);

        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new ForkTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _karmaServiceMock.Verify(x => x.AwardKarmaAsync(
            ownerId,
            userId,
            KarmaEventType.TripForked,
            5,
            tripId,
            KarmaSourceType.Trip), Times.Once);
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            ownerId,
            userId,
            NotificationType.Fork,
            tripId,
            NotificationTargetType.Trip), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetWithAllRelatedDataAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new ForkTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TripNotPublished_ThrowsApiException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = Guid.NewGuid(),
            Status = TripStatus.Draft // Not published
        };

        _tripRepositoryMock.Setup(x => x.GetWithAllRelatedDataAsync(tripId)).ReturnsAsync(trip);

        var command = new ForkTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_SelfFork_ThrowsSelfForkException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId, // Same as userId - self fork
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetWithAllRelatedDataAsync(tripId)).ReturnsAsync(trip);

        var command = new ForkTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<SelfForkException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForkCountIncremented_AtomicWithTripCreation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var originalTrip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published,
            Title = "Test Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure },
            ForkCount = 5,
            Flights = new List<Flight>(),
            Hotels = new List<Hotel>()
        };

        _tripRepositoryMock
            .Setup(x => x.GetWithAllRelatedDataAsync(tripId))
            .ReturnsAsync(originalTrip);

        var trips = new List<Trip>();
        var mockTripsSet = MockDbSetHelper.CreateAsyncMockDbSet(trips);
        mockTripsSet.Setup(x => x.AddAsync(It.IsAny<Trip>(), default))
            .Callback<Trip, CancellationToken>((t, _) => trips.Add(t));
        _contextMock.Setup(x => x.Trips).Returns(mockTripsSet.Object);

        _contextMock.Setup(x => x.TripDestinations).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TripDestination>()).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry>()).Object);
        _contextMock.Setup(x => x.Flights).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Flight>()).Object);
        _contextMock.Setup(x => x.Hotels).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Hotel>()).Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new ForkTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        originalTrip.ForkCount.Should().Be(6); // Incremented by 1
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once); // Single save
    }

    [Fact]
    public async Task Handle_ResetsCounters()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var originalTrip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published,
            Title = "Test Trip",
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure },
            ForkCount = 10,
            UpvoteCount = 20,
            ViewCount = 100,
            PopularityScore = 75.5m,
            Flights = new List<Flight>(),
            Hotels = new List<Hotel>()
        };

        _tripRepositoryMock
            .Setup(x => x.GetWithAllRelatedDataAsync(tripId))
            .ReturnsAsync(originalTrip);

        var trips = new List<Trip>();
        var mockTripsSet = MockDbSetHelper.CreateAsyncMockDbSet(trips);
        mockTripsSet.Setup(x => x.AddAsync(It.IsAny<Trip>(), default))
            .Callback<Trip, CancellationToken>((t, _) => trips.Add(t));
        _contextMock.Setup(x => x.Trips).Returns(mockTripsSet.Object);

        _contextMock.Setup(x => x.TripDestinations).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TripDestination>()).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry>()).Object);
        _contextMock.Setup(x => x.Flights).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Flight>()).Object);
        _contextMock.Setup(x => x.Hotels).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Hotel>()).Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new ForkTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var forkedTrip = trips.FirstOrDefault();
        forkedTrip.Should().NotBeNull();
        forkedTrip!.ForkCount.Should().Be(0);
        forkedTrip.UpvoteCount.Should().Be(0);
        forkedTrip.ViewCount.Should().Be(0);
        forkedTrip.PopularityScore.Should().Be(0);
        forkedTrip.Status.Should().Be(TripStatus.Draft);
    }
}
