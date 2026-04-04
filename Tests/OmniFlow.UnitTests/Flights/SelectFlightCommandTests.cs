using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Flights.Commands.SelectFlight;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Flights;

public class SelectFlightCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IFlightRepositoryAsync> _flightRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly IMapper _mapper;

    public SelectFlightCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _flightRepositoryMock = new Mock<IFlightRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();
        _contextMock = new Mock<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ValidCommand_SelectsFlightAndReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            Status = TripStatus.Draft
        };

        var flight = new Flight
        {
            Id = flightId,
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            ToCity = "Antalya",
            IsBooked = false
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByIdAsync(flightId)).ReturnsAsync(flight);
        _flightRepositoryMock.Setup(x => x.GetBookedFlightsByDirectionAsync(tripId, FlightDirection.Outbound))
            .ReturnsAsync(new List<Flight>());
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = flightId
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        flight.IsBooked.Should().BeTrue();
        flight.BookedAt.Should().NotBeNull();
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonOwner_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = otherUserId,
            Title = "Test Trip"
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = Guid.NewGuid()
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = Guid.NewGuid()
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FlightNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Flight?)null);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = Guid.NewGuid()
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FlightNotBelongingToTrip_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var otherTripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        var flight = new Flight
        {
            Id = Guid.NewGuid(),
            TripId = otherTripId, // Different trip
            FlightDirection = FlightDirection.Outbound
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(flight);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = flight.Id
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyBookedFlight_ReturnsSuccessSilently()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var flightId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        var flight = new Flight
        {
            Id = flightId,
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            IsBooked = true // Already booked
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByIdAsync(flightId)).ReturnsAsync(flight);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = flightId
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithPreviousBooking_CancelsPreviousAndBooksNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var oldFlightId = Guid.NewGuid();
        var newFlightId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        var oldFlight = new Flight
        {
            Id = oldFlightId,
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            IsBooked = true,
            BookedAt = DateTime.UtcNow.AddDays(-1)
        };

        var newFlight = new Flight
        {
            Id = newFlightId,
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            IsBooked = false
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByIdAsync(newFlightId)).ReturnsAsync(newFlight);
        _flightRepositoryMock.Setup(x => x.GetBookedFlightsByDirectionAsync(tripId, FlightDirection.Outbound))
            .ReturnsAsync(new List<Flight> { oldFlight });
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SelectFlightCommand
        {
            TripId = tripId,
            FlightId = newFlightId
        };

        var handler = new SelectFlightCommandHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        // Old flight should be cancelled
        oldFlight.IsBooked.Should().BeFalse();
        oldFlight.BookedAt.Should().BeNull();

        // New flight should be booked
        newFlight.IsBooked.Should().BeTrue();
        newFlight.BookedAt.Should().NotBeNull();

        // Single SaveChanges call (atomic)
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class SelectFlightCommandValidatorTests
{
    private readonly SelectFlightCommandValidator _validator;

    public SelectFlightCommandValidatorTests()
    {
        _validator = new SelectFlightCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        // Arrange
        var command = new SelectFlightCommand
        {
            TripId = Guid.NewGuid(),
            FlightId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTripId_Fails()
    {
        // Arrange
        var command = new SelectFlightCommand
        {
            TripId = Guid.Empty,
            FlightId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SelectFlightCommand.TripId));
    }

    [Fact]
    public void Validate_EmptyFlightId_Fails()
    {
        // Arrange
        var command = new SelectFlightCommand
        {
            TripId = Guid.NewGuid(),
            FlightId = Guid.Empty
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SelectFlightCommand.FlightId));
    }
}