using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.UpvoteTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Features.Trips;

public class UpvoteTripCommandHandlerTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly Mock<IKarmaService> _karmaServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly UpvoteTripCommandHandler _handler;

    public UpvoteTripCommandHandlerTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _karmaServiceMock = new Mock<IKarmaService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _handler = new UpvoteTripCommandHandler(
            _tripRepositoryMock.Object,
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _karmaServiceMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUnit()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid(); // Different from userId
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published,
            UpvoteCount = 0
        };

        _tripRepositoryMock
            .Setup(x => x.GetByIdWithOwnerAsync(tripId))
            .ReturnsAsync(trip);

        // Mock DbSet for TripUpvotes
        var upvotes = new List<TripUpvote>();
        var mockSet = MockDbSetHelper.CreateMockDbSet(upvotes);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new UpvoteTripCommand { TripId = tripId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        trip.UpvoteCount.Should().Be(1);
        _contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _karmaServiceMock.Verify(x => x.AwardKarmaAsync(
            ownerId,
            userId,
            KarmaEventType.TripUpvoted,
            1,
            tripId,
            KarmaSourceType.Trip), Times.Once);
        _notificationServiceMock.Verify(x => x.CreateNotificationAsync(
            ownerId,
            userId,
            NotificationType.TripUpvote,
            tripId,
            NotificationTargetType.Trip), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new UpvoteTripCommand { TripId = tripId };

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

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new UpvoteTripCommand { TripId = tripId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(() => _handler.Handle(command, CancellationToken.None));
        exception.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_SelfUpvote_ThrowsSelfUpvoteException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId, // Same as userId - self upvote
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new UpvoteTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<SelfUpvoteException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateUpvote_ThrowsDuplicateUpvoteException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        // Mock existing upvote
        var existingUpvote = new TripUpvote { TripId = tripId, UserId = userId };
        var upvotes = new List<TripUpvote> { existingUpvote };
        var mockSet = MockDbSetHelper.CreateMockDbSet(upvotes);
        mockSet.Setup(x => x.FindAsync(new object[] { tripId, userId }, default))
            .ReturnsAsync(existingUpvote);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);

        var command = new UpvoteTripCommand { TripId = tripId };

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateUpvoteException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_IncrementUpvoteCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = ownerId,
            Status = TripStatus.Published,
            UpvoteCount = 5 // Starting count
        };

        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var upvotes = new List<TripUpvote>();
        var mockSet = MockDbSetHelper.CreateMockDbSet(upvotes);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockSet.Object);
        _contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new UpvoteTripCommand { TripId = tripId };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        trip.UpvoteCount.Should().Be(6); // Incremented by 1
    }
}