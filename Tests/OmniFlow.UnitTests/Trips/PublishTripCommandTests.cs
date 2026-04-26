using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.PublishTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class PublishTripCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly Mock<IKarmaService> _karmaServiceMock;
    private readonly PublishTripCommandHandler _handler;

    public PublishTripCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _karmaServiceMock = new Mock<IKarmaService>();
        _handler = new PublishTripCommandHandler(
            _tripRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _karmaServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUnit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = TripStatus.Draft
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var timelineEntries = new List<TimelineEntry>
        {
            TimelineEntry.CreatePlaceEntry(tripId, Guid.NewGuid(), 1, 1000, Guid.NewGuid())
        }.AsQueryable();
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateMockDbSet(timelineEntries).Object);

        var command = new PublishTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        trip.Status.Should().Be(TripStatus.Published);
        _karmaServiceMock.Verify(x => x.AwardKarmaAsync(
            userId,
            null,
            KarmaEventType.TripPublished,
            10,
            tripId,
            KarmaSourceType.Trip), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new PublishTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId, // Different from userId
            Status = TripStatus.Draft
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new PublishTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyPublished_ThrowsApiException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = TripStatus.Published // Already published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new PublishTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_NoStops_ThrowsApiException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = TripStatus.Draft
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var timelineEntries = new List<TimelineEntry>().AsQueryable();
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateMockDbSet(timelineEntries).Object);

        var command = new PublishTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
        exception.Message.Should().Contain("timeline");
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mockSet;
    }
}
