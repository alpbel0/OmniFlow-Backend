using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.UnpublishTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class UnpublishTripCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly UnpublishTripCommandHandler _handler;

    public UnpublishTripCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new UnpublishTripCommandHandler(
            _tripRepositoryMock.Object,
            _authenticatedUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_PublishedOwnerTrip_ReturnsUnitAndMovesTripToDraft()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = TripStatus.Published,
            UpvoteCount = 3,
            ForkCount = 2
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var result = await _handler.Handle(new UnpublishTripCommand { TripId = tripId }, CancellationToken.None);

        result.Should().Be(Unit.Value);
        trip.Status.Should().Be(TripStatus.Draft);
        trip.UpvoteCount.Should().Be(3);
        trip.ForkCount.Should().Be(2);
        _tripRepositoryMock.Verify(x => x.UpdateAsync(trip), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new UnpublishTripCommand { TripId = tripId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = Guid.NewGuid(),
            Status = TripStatus.Published
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new UnpublishTripCommand { TripId = tripId }, CancellationToken.None));
    }

    [Theory]
    [InlineData(TripStatus.Draft)]
    [InlineData(TripStatus.Archived)]
    public async Task Handle_NonPublishedTrip_ThrowsApiException(TripStatus status)
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = status
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _handler.Handle(new UnpublishTripCommand { TripId = tripId }, CancellationToken.None));

        exception.StatusCode.Should().Be(400);
        _tripRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }
}
