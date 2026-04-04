using MediatR;
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
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly PublishTripCommandHandler _handler;

    public PublishTripCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new PublishTripCommandHandler(
            _tripRepositoryMock.Object,
            _authenticatedUserServiceMock.Object);
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
            Status = TripStatus.Draft,
            Stops = new List<Stop>
            {
                new() { Id = Guid.NewGuid(), DayNumber = 1 }
            }
        };

        _tripRepositoryMock.Setup(x => x.GetWithStopsAsync(tripId)).ReturnsAsync(trip);

        var command = new PublishTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        trip.Status.Should().Be(TripStatus.Published);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetWithStopsAsync(tripId)).ReturnsAsync((Trip?)null);

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

        _tripRepositoryMock.Setup(x => x.GetWithStopsAsync(tripId)).ReturnsAsync(trip);

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

        _tripRepositoryMock.Setup(x => x.GetWithStopsAsync(tripId)).ReturnsAsync(trip);

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
            Status = TripStatus.Draft,
            Stops = new List<Stop>() // Empty stops
        };

        _tripRepositoryMock.Setup(x => x.GetWithStopsAsync(tripId)).ReturnsAsync(trip);

        var command = new PublishTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
        exception.Message.Should().Contain("stop");
    }
}