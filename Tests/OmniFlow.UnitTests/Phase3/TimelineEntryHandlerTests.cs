using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.TimelineEntries.Commands.CreateTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Commands.DeleteTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Commands.MarkEntryVisited;
using OmniFlow.Application.Features.TimelineEntries.Commands.ReorderTimelineEntries;
using OmniFlow.Application.Features.TimelineEntries.Commands.UpdateTimelineEntry;
using OmniFlow.Application.Features.TimelineEntries.Queries.GetTimeline;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase3;

public class TimelineEntryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ITimelineEntryRepositoryAsync> _timelineRepoMock;
    private readonly Mock<ITimelineService> _timelineServiceMock;
    private readonly Mock<IProviderFlightRepositoryAsync> _providerFlightRepoMock;
    private readonly Mock<IProviderHotelRepositoryAsync> _providerHotelRepoMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    private readonly Mock<IMapper> _mapperMock;

    private readonly CreateTimelineEntryCommandHandler _createHandler;
    private readonly UpdateTimelineEntryCommandHandler _updateHandler;
    private readonly DeleteTimelineEntryCommandHandler _deleteHandler;
    private readonly ReorderTimelineEntriesCommandHandler _reorderHandler;
    private readonly MarkEntryVisitedCommandHandler _markVisitedHandler;
    private readonly GetTimelineQueryHandler _getTimelineHandler;

    public TimelineEntryHandlerTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _timelineRepoMock = new Mock<ITimelineEntryRepositoryAsync>();
        _timelineServiceMock = new Mock<ITimelineService>();
        _providerFlightRepoMock = new Mock<IProviderFlightRepositoryAsync>();
        _providerHotelRepoMock = new Mock<IProviderHotelRepositoryAsync>();
        _authServiceMock = new Mock<IAuthenticatedUserService>();
        _mapperMock = new Mock<IMapper>();

        _createHandler = new CreateTimelineEntryCommandHandler(
            _contextMock.Object,
            _timelineRepoMock.Object,
            _timelineServiceMock.Object,
            _providerFlightRepoMock.Object,
            _providerHotelRepoMock.Object,
            _authServiceMock.Object,
            _mapperMock.Object);
        _updateHandler = new UpdateTimelineEntryCommandHandler(
            _contextMock.Object, _timelineRepoMock.Object, _timelineServiceMock.Object, _authServiceMock.Object, _mapperMock.Object);
        _deleteHandler = new DeleteTimelineEntryCommandHandler(
            _contextMock.Object, _timelineRepoMock.Object, _authServiceMock.Object);
        _reorderHandler = new ReorderTimelineEntriesCommandHandler(
            _contextMock.Object, _timelineRepoMock.Object, _timelineServiceMock.Object, _authServiceMock.Object);
        _markVisitedHandler = new MarkEntryVisitedCommandHandler(
            _contextMock.Object, _timelineRepoMock.Object, _authServiceMock.Object);
        _getTimelineHandler = new GetTimelineQueryHandler(
            _contextMock.Object, _timelineRepoMock.Object, _authServiceMock.Object, _mapperMock.Object);

        _timelineRepoMock.Setup(x => x.AddAsync(It.IsAny<TimelineEntry>())).Returns((TimelineEntry e) => Task.FromResult(e));
        _contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<TimelineEntryResponse>(It.IsAny<TimelineEntry>())).Returns(new TimelineEntryResponse());
        _mapperMock.Setup(x => x.Map<List<TimelineEntryResponse>>(It.IsAny<List<TimelineEntry>>())).Returns(new List<TimelineEntryResponse>());
    }

    private static Trip CreateTestTrip(Guid ownerId, TripStatus status = TripStatus.Draft)
    {
        return new Trip
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Status = status,
            Tempo = Tempo.Moderate,
            Origin = "Istanbul",
            OriginCountry = "Turkey",
            TravelCompanion = TravelCompanion.Solo,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural },
            TransportPreference = TransportPreference.Walking,
            BudgetTier = BudgetTier.Standard,
            Destinations = new List<TripDestination>()
        };
    }

    private static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        return MockDbSetHelper.CreateAsyncMockDbSet(data);
    }

    // ==================================================================
    // Create Handler Tests
    // ==================================================================

    [Fact]
    public async Task Handle_CreatePlaceEntry_ReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());
        _timelineRepoMock.Setup(x => x.GetByIdWithPlaceAsync(It.IsAny<Guid>())).ReturnsAsync((TimelineEntry?)null);

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.Place,
            PlaceId: Guid.NewGuid(), CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        _timelineRepoMock.Verify(x => x.AddAsync(It.IsAny<TimelineEntry>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateCustomFlightEntry_SetsLockedAndBuffer()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomFlight,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: "IST", FlightToAirport: "FCO", FlightFromCity: "Istanbul", FlightToCity: "Rome",
            FlightDepartureAt: new DateTime(2026, 8, 10, 10, 0, 0), FlightArrivalAt: new DateTime(2026, 8, 10, 12, 0, 0), Airline: "TK", FlightNumber: "TK123",
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 200, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e => e.IsLocked && e.BufferMinutes == 120)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateProviderFlightEntry_UsesProviderDataAndLocksEntry()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Paris", "France", 1)
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id
        };
        trip.Destinations.Add(dest);

        var providerFlight = new ProviderFlight
        {
            Id = Guid.NewGuid(),
            DepartureAirportCode = "IST",
            ArrivalAirportCode = "CDG",
            DepartureCity = "Istanbul",
            ArrivalCity = "Paris",
            DepartureTime = new DateTime(2026, 8, 10, 8, 0, 0),
            ArrivalTime = new DateTime(2026, 8, 10, 11, 30, 0),
            Airline = "Turkish Airlines",
            FlightNumber = "TK1001",
            Price = 220,
            CurrencyCode = "EUR"
        };

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _providerFlightRepoMock.Setup(x => x.GetByIdAsync(providerFlight.Id)).ReturnsAsync(providerFlight);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomFlight,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: providerFlight.Id, ProviderHotelId: null, Notes: "Provider booking");

        await _createHandler.Handle(command, CancellationToken.None);

        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e =>
            e.EntryType == TimelineEntryType.CustomFlight &&
            e.ProviderFlightId == providerFlight.Id &&
            e.FlightFromAirport == "IST" &&
            e.FlightToAirport == "CDG" &&
            e.Price == 220 &&
            e.CurrencyCode == "EUR" &&
            e.IsLocked &&
            e.BufferMinutes == 120)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateCustomTransportEntry_SetsLockedAndBuffer()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomTransport,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: new TimeOnly(9, 0), DurationMinutes: 60,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: TransportMode.Train, TransportFromStation: "Roma Termini", TransportToStation: "Florence SMN", TransportCompany: "Trenitalia",
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 50, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e => e.IsLocked && e.BufferMinutes == 30)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateCustomAccommodationEntry_SetsLocked()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomAccommodation,
            PlaceId: null, CustomName: "Hotel Artis", CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: 41.9028, CustomLongitude: 12.4964, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: new DateTime(2026, 8, 10, 14, 0, 0), AccommodationCheckOut: new DateTime(2026, 8, 13, 12, 0, 0), AccommodationAddress: "Via Roma 1",
            Price: 300, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e =>
            e.IsLocked &&
            e.BufferMinutes == 0 &&
            e.CustomLatitude == 41.9028 &&
            e.CustomLongitude == 12.4964)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateProviderHotelEntry_UsesProviderDataAndLocksEntry()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Paris", "France", 1)
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id
        };
        trip.Destinations.Add(dest);

        var providerHotel = new ProviderHotel
        {
            Id = Guid.NewGuid(),
            HotelName = "Budget Paris Inn",
            City = "Paris",
            Country = "France",
            Latitude = 48.8566,
            Longitude = 2.3522,
            PricePerNight = 80,
            CurrencyCode = "USD"
        };

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _providerHotelRepoMock.Setup(x => x.GetByIdAsync(providerHotel.Id)).ReturnsAsync(providerHotel);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomAccommodation,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: providerHotel.Id, Notes: "Provider hotel");

        await _createHandler.Handle(command, CancellationToken.None);

        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e =>
            e.EntryType == TimelineEntryType.CustomAccommodation &&
            e.ProviderHotelId == providerHotel.Id &&
            e.CustomName == "Budget Paris Inn" &&
            e.CustomLatitude == 48.8566 &&
            e.CustomLongitude == 2.3522 &&
            e.AccommodationCheckIn == new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc) &&
            e.AccommodationCheckOut == new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc) &&
            e.Price == 240 &&
            e.CurrencyCode == "USD" &&
            e.IsLocked &&
            e.BufferMinutes == 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateCustomEventEntry_SetsLocked()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.CustomEvent,
            PlaceId: null, CustomName: "Coldplay Concert", CustomCategory: PlaceCategory.Entertainment, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: new TimeOnly(20, 0), DurationMinutes: 180,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 100, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _createHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        _timelineRepoMock.Verify(x => x.AddAsync(It.Is<TimelineEntry>(e => e.IsLocked && e.BufferMinutes == 0)), Times.Once);
    }

    [Fact]
    public async Task Handle_CreateEntry_CapacityExceeded_ThrowsApiException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        trip.Tempo = Tempo.Slow;
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetLastEntryInDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync((TimelineEntry?)null);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(null, null)).Returns(500.0);
        _timelineServiceMock.Setup(x => x.ValidateNewEntry(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<Tempo>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Invalid("Daily capacity exceeded.", "CAPACITY_EXCEEDED"));

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.Place,
            PlaceId: Guid.NewGuid(), CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _createHandler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("capacity");
    }

    [Fact]
    public async Task Handle_CreateEntry_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.Place,
            PlaceId: Guid.NewGuid(), CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _createHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreateEntry_PublishedTrip_ThrowsApiException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Published);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new CreateTimelineEntryCommand(
            TripId: trip.Id, DestinationId: dest.Id, DayNumber: 1, EntryType: TimelineEntryType.Place,
            PlaceId: Guid.NewGuid(), CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _createHandler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("Only draft trips");
    }

    // ==================================================================
    // Update Handler Tests
    // ==================================================================

    [Fact]
    public async Task Handle_UpdateLockedEntry_PriceOnly_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreateCustomFlightEntry(
            trip.Id, dest.Id, 1, 1000,
            "IST", "FCO", new DateTime(2026, 8, 10, 10, 0, 0), new DateTime(2026, 8, 10, 12, 0, 0), price: 200);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.CheckConflict(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest.Id, DayNumber: 1,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 250, CurrencyCode: "EUR", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _updateHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        entry.Price.Should().Be(250);
        entry.CurrencyCode.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_UpdateLockedEntry_FlightTime_ThrowsApiException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreateCustomFlightEntry(
            trip.Id, dest.Id, 1, 1000,
            "IST", "FCO", new DateTime(2026, 8, 10, 10, 0, 0), new DateTime(2026, 8, 10, 12, 0, 0), price: 200);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.CheckConflict(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest.Id, DayNumber: 1,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null,
            FlightDepartureAt: new DateTime(2026, 8, 10, 11, 0, 0), FlightArrivalAt: new DateTime(2026, 8, 10, 13, 0, 0), Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 200, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _updateHandler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("locked");
    }

    [Fact]
    public async Task Handle_UpdateUnlockedEntry_AllFields_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 1000, Guid.NewGuid());
        entry.StartTime = new TimeOnly(10, 0);
        entry.DurationMinutes = 60;

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest.Id, 1)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.CheckConflict(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var newPlaceId = Guid.NewGuid();
        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest.Id, DayNumber: 1,
            PlaceId: newPlaceId, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: new TimeOnly(14, 0), DurationMinutes: 90,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 25, CurrencyCode: "EUR", ProviderFlightId: null, ProviderHotelId: null, Notes: "Updated notes");

        var result = await _updateHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        entry.PlaceId.Should().Be(newPlaceId);
        entry.StartTime.Should().Be(new TimeOnly(14, 0));
        entry.DurationMinutes.Should().Be(90);
        entry.Price.Should().Be(25);
        entry.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task Handle_UpdateEntry_DestinationAndDay_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest1 = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        var dest2 = new TripDestination(new DateOnly(2026, 8, 13), new DateOnly(2026, 8, 17), "Florence", "Italy", 2) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest1);
        trip.Destinations.Add(dest2);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest1.Id, 1, 1000, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAndDayAsync(trip.Id, dest2.Id, 2)).ReturnsAsync(new List<TimelineEntry>());
        _timelineServiceMock.Setup(x => x.CheckConflict(It.IsAny<TimelineEntry>(), It.IsAny<IEnumerable<TimelineEntry>>(), It.IsAny<DateOnly>())).Returns(TimelineValidationResult.Valid());

        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest2.Id, DayNumber: 2,
            PlaceId: entry.PlaceId, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var result = await _updateHandler.Handle(command, CancellationToken.None);
        result.Should().NotBeNull();
        entry.DestinationId.Should().Be(dest2.Id);
        entry.DayNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UpdateEntry_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 1000, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest.Id, DayNumber: 1,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _updateHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UpdateEntry_PublishedTrip_ThrowsApiException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Published);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 1000, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new UpdateTimelineEntryCommand(
            Id: entry.Id, DestinationId: dest.Id, DayNumber: 1,
            PlaceId: null, CustomName: null, CustomCategory: null, CustomPhotoUrl: null, CustomLatitude: null, CustomLongitude: null, CustomDescription: null,
            StartTime: null, DurationMinutes: null,
            FlightFromAirport: null, FlightToAirport: null, FlightFromCity: null, FlightToCity: null, FlightDepartureAt: null, FlightArrivalAt: null, Airline: null, FlightNumber: null,
            TransportType: null, TransportFromStation: null, TransportToStation: null, TransportCompany: null,
            AccommodationCheckIn: null, AccommodationCheckOut: null, AccommodationAddress: null,
            Price: 0, CurrencyCode: "USD", ProviderFlightId: null, ProviderHotelId: null, Notes: null);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _updateHandler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("Only draft trips");
    }

    // ==================================================================
    // Delete Handler Tests
    // ==================================================================

    [Fact]
    public async Task Handle_DeleteUnlockedEntry_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 1000, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateAsyncMockDbSet(new List<TimelineEntry>()).Object);

        var command = new DeleteTimelineEntryCommand(entry.Id);

        var result = await _deleteHandler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        entry.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_DeleteLockedEntry_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreateCustomFlightEntry(
            trip.Id, dest.Id, 1, 1000,
            "IST", "FCO", new DateTime(2026, 8, 10, 10, 0, 0), new DateTime(2026, 8, 10, 12, 0, 0));

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new DeleteTimelineEntryCommand(entry.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => _deleteHandler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DeleteEntry_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 1000, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new DeleteTimelineEntryCommand(entry.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => _deleteHandler.Handle(command, CancellationToken.None));
    }

    // ==================================================================
    // Reorder Handler Tests
    // ==================================================================

    [Fact]
    public async Task Handle_Reorder_BetweenTwoEntries_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry1 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());
        var entry2 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 200, Guid.NewGuid());
        var entry3 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 300, Guid.NewGuid());

        var allEntries = new List<TimelineEntry> { entry1, entry2, entry3 };

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry2.Id)).ReturnsAsync(entry2);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateAsyncMockDbSet(allEntries).Object);
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(100.0, 300.0)).Returns(200.0);

        var command = new ReorderTimelineEntriesCommand(trip.Id, dest.Id, entry2.Id, BeforeEntryId: entry3.Id, AfterEntryId: entry1.Id);

        var result = await _reorderHandler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        entry2.OrderIndex.Should().Be(200.0);
    }

    [Fact]
    public async Task Handle_Reorder_ToEnd_Success()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry1 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());
        var entry2 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 200, Guid.NewGuid());

        var allEntries = new List<TimelineEntry> { entry1, entry2 };

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry1.Id)).ReturnsAsync(entry1);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateAsyncMockDbSet(allEntries).Object);
        _timelineServiceMock.Setup(x => x.GetLexoRankBetween(200.0, null)).Returns(700.0);

        var command = new ReorderTimelineEntriesCommand(trip.Id, dest.Id, entry1.Id, BeforeEntryId: null, AfterEntryId: entry2.Id);

        var result = await _reorderHandler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        entry1.OrderIndex.Should().Be(700.0);
    }

    [Fact]
    public async Task Handle_Reorder_DifferentDay_ThrowsApiException()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry1 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());
        var entry2 = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 2, 100, Guid.NewGuid());

        var allEntries = new List<TimelineEntry> { entry1, entry2 };

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry1.Id)).ReturnsAsync(entry1);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _contextMock.Setup(x => x.TimelineEntries).Returns(CreateAsyncMockDbSet(allEntries).Object);

        var command = new ReorderTimelineEntriesCommand(trip.Id, dest.Id, entry1.Id, BeforeEntryId: entry2.Id, AfterEntryId: null);

        var ex = await Assert.ThrowsAsync<ApiException>(() => _reorderHandler.Handle(command, CancellationToken.None));
        ex.Message.Should().Contain("same destination and day");
    }

    [Fact]
    public async Task Handle_Reorder_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new ReorderTimelineEntriesCommand(trip.Id, dest.Id, entry.Id, null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => _reorderHandler.Handle(command, CancellationToken.None));
    }

    // ==================================================================
    // MarkVisited Handler Tests
    // ==================================================================

    [Fact]
    public async Task Handle_MarkVisited_SetsVisitedAt()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Published);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new MarkEntryVisitedCommand(entry.Id, true);

        var result = await _markVisitedHandler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        entry.IsVisited.Should().BeTrue();
        entry.VisitedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_MarkUnvisited_ClearsVisitedAt()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Published);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());
        entry.MarkVisited();

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new MarkEntryVisitedCommand(entry.Id, false);

        var result = await _markVisitedHandler.Handle(command, CancellationToken.None);
        result.Should().Be(Unit.Value);
        entry.IsVisited.Should().BeFalse();
        entry.VisitedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MarkVisited_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId, TripStatus.Published);
        var dest = new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Rome", "Italy", 1) { Id = Guid.NewGuid(), TripId = trip.Id };
        trip.Destinations.Add(dest);

        var entry = TimelineEntry.CreatePlaceEntry(trip.Id, dest.Id, 1, 100, Guid.NewGuid());

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _timelineRepoMock.Setup(x => x.GetByIdAsync(entry.Id)).ReturnsAsync(entry);
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var command = new MarkEntryVisitedCommand(entry.Id, true);

        await Assert.ThrowsAsync<ForbiddenException>(() => _markVisitedHandler.Handle(command, CancellationToken.None));
    }

    // ==================================================================
    // GetTimeline Query Tests
    // ==================================================================

    [Fact]
    public async Task Handle_GetTimeline_PublishedTrip_ReturnsAllEntries()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Published);

        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAsync(trip.Id)).ReturnsAsync(new List<TimelineEntry>());

        var query = new GetTimelineQuery(trip.Id);
        var result = await _getTimelineHandler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetTimeline_DraftTrip_Owner_ReturnsEntries()
    {
        var userId = Guid.NewGuid();
        var trip = CreateTestTrip(userId, TripStatus.Draft);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);
        _timelineRepoMock.Setup(x => x.GetByTripAsync(trip.Id)).ReturnsAsync(new List<TimelineEntry>());

        var query = new GetTimelineQuery(trip.Id);
        var result = await _getTimelineHandler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_GetTimeline_DraftTrip_NotOwner_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var trip = CreateTestTrip(otherUserId, TripStatus.Draft);

        _authServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
        _contextMock.Setup(x => x.Trips).Returns(CreateAsyncMockDbSet(new List<Trip> { trip }).Object);

        var query = new GetTimelineQuery(trip.Id);
        await Assert.ThrowsAsync<ForbiddenException>(() => _getTimelineHandler.Handle(query, CancellationToken.None));
    }
}
