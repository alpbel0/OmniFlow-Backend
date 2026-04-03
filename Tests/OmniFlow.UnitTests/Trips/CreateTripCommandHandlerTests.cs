using AutoMapper;
using Moq;
using OmniFlow.Application.Features.Trips.Commands.CreateTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class CreateTripCommandHandlerTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly CreateTripCommandHandler _handler;

    public CreateTripCommandHandlerTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _mapperMock = new Mock<IMapper>();
        _handler = new CreateTripCommandHandler(
            _tripRepositoryMock.Object,
            _authenticatedUserServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTripId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        var mappedTrip = new Trip
        {
            Id = Guid.NewGuid(),
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };

        _mapperMock
            .Setup(x => x.Map<Trip>(command))
            .Returns(mappedTrip);

        _tripRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Trip>()))
            .ReturnsAsync(mappedTrip);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(mappedTrip.Id);
        _tripRepositoryMock.Verify(x => x.AddAsync(It.Is<Trip>(t =>
            t.OwnerId == userId &&
            t.Status == TripStatus.Draft)), Times.Once);
    }

    [Fact]
    public async Task Handle_SetsOwnerIdFromAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        var mappedTrip = new Trip { Id = Guid.NewGuid() };

        _mapperMock.Setup(x => x.Map<Trip>(command)).Returns(mappedTrip);
        _tripRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Trip>())).ReturnsAsync(mappedTrip);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mappedTrip.OwnerId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_SetsStatusToDraft()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        var mappedTrip = new Trip { Id = Guid.NewGuid() };

        _mapperMock.Setup(x => x.Map<Trip>(command)).Returns(mappedTrip);
        _tripRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Trip>())).ReturnsAsync(mappedTrip);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        mappedTrip.Status.Should().Be(TripStatus.Draft);
    }
}