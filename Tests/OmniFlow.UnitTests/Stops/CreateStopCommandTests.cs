using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Stops.Commands.CreateStop;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Stops;

public class CreateStopCommandTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IStopRepositoryAsync> _stopRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;
    private readonly IMapper _mapper;

    public CreateStopCommandTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _stopRepositoryMock = new Mock<IStopRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesStopAndReturnsId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var placeId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            Status = TripStatus.Draft
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetLastStopInDayAsync(tripId, 1)).ReturnsAsync((Stop?)null);
        _stopRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Stop>())).ReturnsAsync((Stop s) => s);

        var command = new CreateStopCommand
        {
            TripId = tripId,
            PlaceId = placeId,
            DayNumber = 1,
            DurationMinutes = 60,
            ActivityPrice = 100,
            TransportPrice = 50,
            CurrencyCode = "USD"
        };

        var handler = new CreateStopCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);
        _stopRepositoryMock.Verify(x => x.AddAsync(It.Is<Stop>(s =>
            s.TripId == tripId &&
            s.PlaceId == placeId &&
            s.DayNumber == 1 &&
            s.OrderIndex == 1000.0 &&
            s.AddedBy == StopAddedBy.User)), Times.Once);
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

        var command = new CreateStopCommand
        {
            TripId = tripId,
            PlaceId = Guid.NewGuid(),
            DayNumber = 1
        };

        var handler = new CreateStopCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

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

        var command = new CreateStopCommand
        {
            TripId = tripId,
            PlaceId = Guid.NewGuid(),
            DayNumber = 1
        };

        var handler = new CreateStopCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithExistingStops_CalculatesCorrectOrderIndex()
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

        var lastStop = new Stop
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            DayNumber = 1,
            OrderIndex = 3000.0
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _stopRepositoryMock.Setup(x => x.GetLastStopInDayAsync(tripId, 1)).ReturnsAsync(lastStop);
        _stopRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Stop>())).ReturnsAsync((Stop s) => s);

        var command = new CreateStopCommand
        {
            TripId = tripId,
            PlaceId = Guid.NewGuid(),
            DayNumber = 1
        };

        var handler = new CreateStopCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _stopRepositoryMock.Verify(x => x.AddAsync(It.Is<Stop>(s => s.OrderIndex == 4000.0)), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomName_SetsCustomFields()
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
        _stopRepositoryMock.Setup(x => x.GetLastStopInDayAsync(tripId, 1)).ReturnsAsync((Stop?)null);
        _stopRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Stop>())).ReturnsAsync((Stop s) => s);

        var command = new CreateStopCommand
        {
            TripId = tripId,
            CustomName = "Custom Stop Name",
            CustomCategory = PlaceCategory.Restaurant,
            DayNumber = 1
        };

        var handler = new CreateStopCommandHandler(
            _tripRepositoryMock.Object,
            _stopRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        _stopRepositoryMock.Verify(x => x.AddAsync(It.Is<Stop>(s =>
            s.CustomName == "Custom Stop Name" &&
            s.CustomCategory == PlaceCategory.Restaurant)), Times.Once);
    }
}

public class CreateStopCommandValidatorTests
{
    private readonly CreateStopCommandValidator _validator;

    public CreateStopCommandValidatorTests()
    {
        _validator = new CreateStopCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = Guid.NewGuid(),
            DayNumber = 1,
            DurationMinutes = 60,
            ActivityPrice = 100,
            CurrencyCode = "USD"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DayNumberZeroOrNegative_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = Guid.NewGuid(),
            DayNumber = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStopCommand.DayNumber));
    }

    [Fact]
    public void Validate_NegativeActivityPrice_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = Guid.NewGuid(),
            DayNumber = 1,
            ActivityPrice = -10
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStopCommand.ActivityPrice));
    }

    [Fact]
    public void Validate_NoPlaceIdAndNoCustomName_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            DayNumber = 1
            // No PlaceId and no CustomName
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Either PlaceId or CustomName"));
    }

    [Fact]
    public void Validate_CustomNameWithoutCustomCategory_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            DayNumber = 1,
            CustomName = "Custom Stop"
            // CustomCategory is missing
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStopCommand.CustomCategory));
    }

    [Fact]
    public void Validate_TimeLockedWithoutArrivalTime_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = Guid.NewGuid(),
            DayNumber = 1,
            IsTimeLocked = true
            // ArrivalTime is missing
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStopCommand.ArrivalTime));
    }

    [Fact]
    public void Validate_FallbackPlaceIdEqualsPlaceId_Fails()
    {
        // Arrange
        var placeId = Guid.NewGuid();
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = placeId,
            FallbackPlaceId = placeId, // Same as PlaceId
            DayNumber = 1
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("FallbackPlaceId must be different"));
    }

    [Fact]
    public void Validate_InvalidCurrencyCode_Fails()
    {
        // Arrange
        var command = new CreateStopCommand
        {
            TripId = Guid.NewGuid(),
            PlaceId = Guid.NewGuid(),
            DayNumber = 1,
            CurrencyCode = "US" // Should be 3 letters
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStopCommand.CurrencyCode));
    }
}