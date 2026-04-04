using AutoMapper;
using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Stops.Commands.ReorderStops;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Stops;

public class ReorderStopsCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IStopRepositoryAsync> _stopRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;

    public ReorderStopsCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _stopRepositoryMock = new Mock<IStopRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();
    }

    [Fact]
    public async Task Handle_ValidReorder_UpdatesOrderIndex()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId1 = Guid.NewGuid();
        var stopId2 = Guid.NewGuid();
        var stopId3 = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };
        var stops = new List<Stop>
        {
            new() { Id = stopId1, TripId = tripId, DayNumber = 1, OrderIndex = 1000.0 },
            new() { Id = stopId2, TripId = tripId, DayNumber = 1, OrderIndex = 2000.0 },
            new() { Id = stopId3, TripId = tripId, DayNumber = 1, OrderIndex = 3000.0 }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByTripAsync(tripId)).ReturnsAsync(stops);
        _stopRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Stop>())).Returns(Task.CompletedTask);

        var command = new ReorderStopsCommand
        {
            TripId = tripId,
            Items = new List<ReorderStopItem>
            {
                new()
                {
                    StopId = stopId3,
                    NewDayNumber = 1,
                    AfterStopId = stopId1,
                    BeforeStopId = stopId2
                }
            }
        };

        var handler = new ReorderStopsCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - OrderIndex should be (1000 + 2000) / 2 = 1500
        _stopRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Stop>(s =>
            s.Id == stopId3 && s.OrderIndex == 1500.0)), Times.Once);
    }

    [Fact]
    public async Task Handle_TimeLockedStop_ThrowsApiException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };
        var stops = new List<Stop>
        {
            new() { Id = stopId, TripId = tripId, DayNumber = 1, OrderIndex = 1000.0, IsTimeLocked = true }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByTripAsync(tripId)).ReturnsAsync(stops);

        var command = new ReorderStopsCommand
        {
            TripId = tripId,
            Items = new List<ReorderStopItem>
            {
                new() { StopId = stopId, NewDayNumber = 1 }
            }
        };

        var handler = new ReorderStopsCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = otherUserId };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new ReorderStopsCommand
        {
            TripId = tripId,
            Items = new List<ReorderStopItem> { new() { StopId = Guid.NewGuid(), NewDayNumber = 1 } }
        };

        var handler = new ReorderStopsCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InsertAfterLast_CalculatesCorrectOrderIndex()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var stopId1 = Guid.NewGuid();
        var stopId2 = Guid.NewGuid();

        var trip = new Trip { Id = tripId, OwnerId = userId };
        var stops = new List<Stop>
        {
            new() { Id = stopId1, TripId = tripId, DayNumber = 1, OrderIndex = 1000.0 },
            new() { Id = stopId2, TripId = tripId, DayNumber = 1, OrderIndex = 2000.0 }
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetByTripAsync(tripId)).ReturnsAsync(stops);
        _stopRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Stop>())).Returns(Task.CompletedTask);

        var command = new ReorderStopsCommand
        {
            TripId = tripId,
            Items = new List<ReorderStopItem>
            {
                new() { StopId = stopId1, NewDayNumber = 1, AfterStopId = stopId2 }
            }
        };

        var handler = new ReorderStopsCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - OrderIndex should be 2000 + 500 = 2500
        _stopRepositoryMock.Verify(x => x.UpdateAsync(It.Is<Stop>(s =>
            s.Id == stopId1 && s.OrderIndex == 2500.0)), Times.Once);
    }
}

public class ReorderStopsCommandValidatorTests
{
    private readonly ReorderStopsCommandValidator _validator;

    public ReorderStopsCommandValidatorTests()
    {
        _validator = new ReorderStopsCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        // Arrange
        var command = new ReorderStopsCommand
        {
            TripId = Guid.NewGuid(),
            Items = new List<ReorderStopItem>
            {
                new() { StopId = Guid.NewGuid(), NewDayNumber = 1 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyItems_Fails()
    {
        // Arrange
        var command = new ReorderStopsCommand
        {
            TripId = Guid.NewGuid(),
            Items = new List<ReorderStopItem>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ZeroDayNumber_Fails()
    {
        // Arrange
        var command = new ReorderStopsCommand
        {
            TripId = Guid.NewGuid(),
            Items = new List<ReorderStopItem>
            {
                new() { StopId = Guid.NewGuid(), NewDayNumber = 0 }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}