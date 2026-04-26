using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Hotels.Commands.SelectHotel;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Hotels;

public class SelectHotelCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IHotelRepositoryAsync> _hotelRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly IMapper _mapper;

    public SelectHotelCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _hotelRepositoryMock = new Mock<IHotelRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();
        _contextMock = new Mock<IApplicationDbContext>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ValidCommand_SelectsHotelAndReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            Status = TripStatus.Draft
        };

        var hotel = new Hotel
        {
            Id = hotelId,
            TripId = tripId,
            HotelName = "Test Hotel",
            CheckIn = DateTime.Today.AddDays(7),
            CheckOut = DateTime.Today.AddDays(10),
            IsBooked = false
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _hotelRepositoryMock.Setup(x => x.GetByIdAsync(hotelId)).ReturnsAsync(hotel);
        _hotelRepositoryMock.Setup(x => x.GetBookedHotelsByTripAsync(tripId))
            .ReturnsAsync(new List<Hotel>());
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = hotelId
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        hotel.IsBooked.Should().BeTrue();
        hotel.BookedAt.Should().NotBeNull();
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

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = Guid.NewGuid()
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
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

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = Guid.NewGuid()
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsEntityNotFoundException()
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
        _hotelRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Hotel?)null);

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = Guid.NewGuid()
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_HotelNotBelongingToTrip_ThrowsForbiddenException()
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

        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            TripId = otherTripId, // Different trip
            HotelName = "Test Hotel"
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _hotelRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(hotel);

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = hotel.Id
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyBookedHotel_ReturnsSuccessSilently()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var hotelId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        var hotel = new Hotel
        {
            Id = hotelId,
            TripId = tripId,
            HotelName = "Test Hotel",
            IsBooked = true // Already booked
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _hotelRepositoryMock.Setup(x => x.GetByIdAsync(hotelId)).ReturnsAsync(hotel);

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = hotelId
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
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
        var oldHotelId = Guid.NewGuid();
        var newHotelId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip"
        };

        var oldHotel = new Hotel
        {
            Id = oldHotelId,
            TripId = tripId,
            HotelName = "Old Hotel",
            IsBooked = true,
            BookedAt = DateTime.UtcNow.AddDays(-1)
        };

        var newHotel = new Hotel
        {
            Id = newHotelId,
            TripId = tripId,
            HotelName = "New Hotel",
            IsBooked = false
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _hotelRepositoryMock.Setup(x => x.GetByIdAsync(newHotelId)).ReturnsAsync(newHotel);
        _hotelRepositoryMock.Setup(x => x.GetBookedHotelsByTripAsync(tripId))
            .ReturnsAsync(new List<Hotel> { oldHotel });
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var command = new SelectHotelCommand
        {
            TripId = tripId,
            HotelId = newHotelId
        };

        var handler = new SelectHotelCommandHandler(
            _tripRepositoryMock.Object,
            _hotelRepositoryMock.Object,
            _userServiceMock.Object,
            _contextMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);

        // Old hotel should be cancelled
        oldHotel.IsBooked.Should().BeFalse();
        oldHotel.BookedAt.Should().BeNull();

        // New hotel should be booked
        newHotel.IsBooked.Should().BeTrue();
        newHotel.BookedAt.Should().NotBeNull();

        // Single SaveChanges call (atomic)
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class SelectHotelCommandValidatorTests
{
    private readonly SelectHotelCommandValidator _validator;

    public SelectHotelCommandValidatorTests()
    {
        _validator = new SelectHotelCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        // Arrange
        var command = new SelectHotelCommand
        {
            TripId = Guid.NewGuid(),
            HotelId = Guid.NewGuid()
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
        var command = new SelectHotelCommand
        {
            TripId = Guid.Empty,
            HotelId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SelectHotelCommand.TripId));
    }

    [Fact]
    public void Validate_EmptyHotelId_Fails()
    {
        // Arrange
        var command = new SelectHotelCommand
        {
            TripId = Guid.NewGuid(),
            HotelId = Guid.Empty
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SelectHotelCommand.HotelId));
    }
}