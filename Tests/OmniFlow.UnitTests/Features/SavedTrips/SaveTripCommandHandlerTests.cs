using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.SavedTrips.Commands.SaveTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Features.SavedTrips;

public class SaveTripCommandHandlerTests
{
    private readonly Mock<IGenericRepositoryAsync<Trip>> _tripRepositoryMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly SaveTripCommandHandler _handler;

    public SaveTripCommandHandlerTests()
    {
        _tripRepositoryMock = new Mock<IGenericRepositoryAsync<Trip>>();
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new SaveTripCommandHandler(
            _tripRepositoryMock.Object,
            _contextMock.Object,
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
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var savedTrips = new List<SavedTrip>();
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync((SavedTrip?)null);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new SaveTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        mockSet.Verify(x => x.Add(It.IsAny<SavedTrip>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new SaveTripCommand { TripId = tripId };

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
            Status = TripStatus.Archived
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var command = new SaveTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_SelfSaveAllowed_ReturnsUnit()
    {
        // Arrange - User saving their own trip is allowed
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId, // Same as userId - self save allowed
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var savedTrips = new List<SavedTrip>();
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync((SavedTrip?)null);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new SaveTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - No exception, save succeeds
        result.Should().Be(Unit.Value);
        mockSet.Verify(x => x.Add(It.IsAny<SavedTrip>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateSave_SilentIgnore()
    {
        // Arrange - Duplicate save should silently ignore
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        // Existing saved trip
        var existingSave = new SavedTrip { UserId = userId, TripId = tripId };
        var savedTrips = new List<SavedTrip> { existingSave };
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync(existingSave);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);

        var command = new SaveTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Returns Unit without error, no new save added
        result.Should().Be(Unit.Value);
        mockSet.Verify(x => x.Add(It.IsAny<SavedTrip>()), Times.Never);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateSave_NoNewRecordCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip { Id = tripId, Status = TripStatus.Published };
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var existingSave = new SavedTrip { UserId = userId, TripId = tripId };
        var savedTrips = new List<SavedTrip> { existingSave };
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync(existingSave);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);

        var command = new SaveTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - List count unchanged
        savedTrips.Count.Should().Be(1); // Only the existing one
    }
}
