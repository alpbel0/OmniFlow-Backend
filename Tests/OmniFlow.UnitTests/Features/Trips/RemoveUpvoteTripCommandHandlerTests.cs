using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.RemoveUpvoteTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Features.Trips;

public class RemoveUpvoteTripCommandHandlerTests
{
    private readonly Mock<IGenericRepositoryAsync<Trip>> _tripRepositoryMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly RemoveUpvoteTripCommandHandler _handler;

    public RemoveUpvoteTripCommandHandlerTests()
    {
        _tripRepositoryMock = new Mock<IGenericRepositoryAsync<Trip>>();
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new RemoveUpvoteTripCommandHandler(
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
            UpvoteCount = 5
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var existingUpvote = new TripUpvote { TripId = tripId, UserId = userId };
        var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TripUpvote> { existingUpvote });
        mockSet.Setup(x => x.FindAsync(new object[] { tripId, userId }, default))
            .ReturnsAsync(existingUpvote);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new RemoveUpvoteTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        trip.UpvoteCount.Should().Be(4);
        mockSet.Verify(x => x.Remove(existingUpvote), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new RemoveUpvoteTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpvoteNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip { Id = tripId, UpvoteCount = 0 };
        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TripUpvote>());
        mockSet.Setup(x => x.FindAsync(new object[] { tripId, userId }, default))
            .ReturnsAsync((TripUpvote?)null);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);

        var command = new RemoveUpvoteTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DecrementUpvoteCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            UpvoteCount = 10 // Starting count
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var existingUpvote = new TripUpvote { TripId = tripId, UserId = userId };
        var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TripUpvote> { existingUpvote });
        mockSet.Setup(x => x.FindAsync(new object[] { tripId, userId }, default))
            .ReturnsAsync(existingUpvote);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new RemoveUpvoteTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        trip.UpvoteCount.Should().Be(9); // Decrement by 1
    }

    [Fact]
    public async Task Handle_UpvoteCountDoesNotGoBelowZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            UpvoteCount = 0 // Already at zero
        };

        _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId)).ReturnsAsync(trip);

        var existingUpvote = new TripUpvote { TripId = tripId, UserId = userId };
        var mockSet = MockDbSetHelper.CreateMockDbSet(new List<TripUpvote> { existingUpvote });
        mockSet.Setup(x => x.FindAsync(new object[] { tripId, userId }, default))
            .ReturnsAsync(existingUpvote);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new RemoveUpvoteTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        trip.UpvoteCount.Should().Be(0); // Clamped at 0
    }
}