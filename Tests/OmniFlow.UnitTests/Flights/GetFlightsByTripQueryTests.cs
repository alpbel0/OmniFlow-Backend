using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.Flights;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Flights;

public class GetFlightsByTripQueryTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock;
    private readonly Mock<IFlightRepositoryAsync> _flightRepositoryMock;
    private readonly Mock<IAuthenticatedUserService> _userServiceMock;
    private readonly IMapper _mapper;

    public GetFlightsByTripQueryTests()
    {
        _tripRepositoryMock = new Mock<ITripRepositoryAsync>();
        _flightRepositoryMock = new Mock<IFlightRepositoryAsync>();
        _userServiceMock = new Mock<IAuthenticatedUserService>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsGroupedFlights()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            Status = TripStatus.Published
        };

        var outboundFlight1 = new Flight
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            ToCity = "Antalya",
            DepartureAt = DateTime.UtcNow.AddDays(7),
            IsBooked = true
        };

        var outboundFlight2 = new Flight
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            ToCity = "Antalya",
            DepartureAt = DateTime.UtcNow.AddDays(8),
            IsBooked = false
        };

        var returnFlight1 = new Flight
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            FlightDirection = FlightDirection.Return,
            FromCity = "Antalya",
            ToCity = "Istanbul",
            DepartureAt = DateTime.UtcNow.AddDays(14),
            IsBooked = true
        };

        var flights = new List<Flight> { outboundFlight1, outboundFlight2, returnFlight1 };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByTripAsync(tripId, null)).ReturnsAsync(flights);

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OutboundFlights.Should().HaveCount(2);
        result.ReturnFlights.Should().HaveCount(1);

        result.OutboundFlights[0].FromCity.Should().Be("Istanbul");
        result.ReturnFlights[0].FromCity.Should().Be("Antalya");
    }

    [Fact]
    public async Task Handle_PublishedTrip_PublicAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = otherUserId, // Different owner
            Title = "Test Trip",
            Status = TripStatus.Published // Published = public
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByTripAsync(tripId, null)).ReturnsAsync(new List<Flight>());

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - Should not throw exception
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DraftTrip_OwnerOnly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = otherUserId, // Different owner
            Title = "Test Trip",
            Status = TripStatus.Draft // Draft = owner-only
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DraftTrip_OwnerCanAccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId, // Same as requesting user
            Title = "Test Trip",
            Status = TripStatus.Draft
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByTripAsync(tripId, null)).ReturnsAsync(new List<Flight>());

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_TripNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var tripId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync((Trip?)null);

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmptyFlightList_ReturnsEmptyViewModel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        var trip = new Trip
        {
            Id = tripId,
            OwnerId = userId,
            Title = "Test Trip",
            Status = TripStatus.Published
        };

        _userServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _tripRepositoryMock.Setup(x => x.GetByIdWithOwnerAsync(tripId)).ReturnsAsync(trip);
        _flightRepositoryMock.Setup(x => x.GetByTripAsync(tripId, null)).ReturnsAsync(new List<Flight>());

        var query = new GetFlightsByTripQuery(tripId);

        var handler = new GetFlightsByTripQueryHandler(
            _tripRepositoryMock.Object,
            _flightRepositoryMock.Object,
            _userServiceMock.Object,
            _mapper);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.OutboundFlights.Should().BeEmpty();
        result.ReturnFlights.Should().BeEmpty();
    }
}