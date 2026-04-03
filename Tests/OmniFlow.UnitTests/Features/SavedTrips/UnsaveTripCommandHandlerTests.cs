using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.SavedTrips.Commands.UnsaveTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Features.SavedTrips;

public class UnsaveTripCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly UnsaveTripCommandHandler _handler;

    public UnsaveTripCommandHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new UnsaveTripCommandHandler(
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

        var existingSave = new SavedTrip { UserId = userId, TripId = tripId };
        var savedTrips = new List<SavedTrip> { existingSave };
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync(existingSave);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new UnsaveTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        mockSet.Verify(x => x.Remove(existingSave), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_SavedTripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var savedTrips = new List<SavedTrip>();
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync((SavedTrip?)null);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);

        var command = new UnsaveTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RemovesSavedTripRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var existingSave = new SavedTrip { UserId = userId, TripId = tripId };
        var savedTrips = new List<SavedTrip> { existingSave };
        var mockSet = MockDbSetHelper.CreateMockDbSet(savedTrips);
        mockSet.Setup(x => x.FindAsync(new object[] { userId, tripId }, default))
            .ReturnsAsync(existingSave);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new UnsaveTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mockSet.Verify(x => x.Remove(It.Is<SavedTrip>(s => s.UserId == userId && s.TripId == tripId)), Times.Once);
    }
}