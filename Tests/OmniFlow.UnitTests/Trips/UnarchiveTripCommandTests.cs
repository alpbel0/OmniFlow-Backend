using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Commands.UnarchiveTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class UnarchiveTripCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly UnarchiveTripCommandHandler _handler;

    public UnarchiveTripCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _handler = new UnarchiveTripCommandHandler(
            _tripRepositoryMock.Object,
            _authenticatedUserServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ArchivedOwnerTrip_ReturnsUnitAndPublishesTrip()
    {
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Status = TripStatus.Archived
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var result = await _handler.Handle(new UnarchiveTripCommand { TripId = tripId }, CancellationToken.None);

        result.Should().Be(Unit.Value);
        trip.Status.Should().Be(TripStatus.Published);
        _tripRepositoryMock.Verify(x => x.UpdateAsync(trip), Times.Once);
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        var tripId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _handler.Handle(new UnarchiveTripCommand { TripId = tripId }, CancellationToken.None));
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
            Status = TripStatus.Archived
        };

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new UnarchiveTripCommand { TripId = tripId }, CancellationToken.None));
    }

    [Theory]
    [InlineData(TripStatus.Draft)]
    [InlineData(TripStatus.Published)]
    public async Task Handle_NonArchivedTrip_ThrowsApiException(TripStatus status)
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
            _handler.Handle(new UnarchiveTripCommand { TripId = tripId }, CancellationToken.None));

        exception.StatusCode.Should().Be(400);
        _tripRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Trip>()), Times.Never);
    }
}
