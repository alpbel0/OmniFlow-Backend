using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Stops.Commands.MarkStopVisited;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Stops;

public class MarkStopVisitedCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IStopRepositoryAsync> _stopRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;

    public MarkStopVisitedCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _stopRepositoryMock = new Mock<IStopRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsIsVisitedAndVisitedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };
        var stop = new Stop
        {
            Id = stopId,
            TripId = tripId,
            DayNumber = 1,
            OrderIndex = 1000.0,
            IsVisited = false,
            VisitedAt = null
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByIdAsync(stopId)).ReturnsAsync(stop);
        _stopRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Stop>())).Returns(Task.CompletedTask);

        var command = new MarkStopVisitedCommand { TripId = tripId, StopId = stopId };

        var handler = new MarkStopVisitedCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _stopRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Stop>(s =>
            s.IsVisited == true &&
            s.VisitedAt != null)), Times.Once);
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = otherUserId };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new MarkStopVisitedCommand { TripId = tripId, StopId = stopId };

        var handler = new MarkStopVisitedCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StopNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByIdAsync(stopId)).ReturnsAsync((Stop?)null);

        var command = new MarkStopVisitedCommand { TripId = tripId, StopId = stopId };

        var handler = new MarkStopVisitedCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StopNotInTrip_ThrowsApiException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var otherTripId = Guid.NewGuid();
        var stopId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };
        var stop = new Stop { Id = stopId, TripId = otherTripId };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByIdAsync(stopId)).ReturnsAsync(stop);

        var command = new MarkStopVisitedCommand { TripId = tripId, StopId = stopId };

        var handler = new MarkStopVisitedCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(command, CancellationToken.None));
    }
}