using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Parameters;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Tests;

public class FlightRepositoryTest : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private FlightRepositoryAsync _flightRepository = null!;
    private TripRepositoryAsync _tripRepository = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly string _connectionString = "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres";

    private static DateTime GetTestDateTime(int daysToAdd)
    {
        // FlightConfiguration uses 'timestamp without time zone' for DepartureAt and ArrivalAt
        // This requires DateTimeKind.Unspecified
        return DateTime.SpecifyKind(DateTime.Today.AddDays(daysToAdd), DateTimeKind.Unspecified);
    }

    private static DateTime GetUtcDateTime(int daysToAdd = 0)
    {
        // For columns that use 'timestamp with time zone' (BookedAt, DataFetchedAt)
        // This requires DateTimeKind.Utc
        return DateTime.UtcNow.Date.AddDays(daysToAdd);
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new ApplicationDbContext(options);

        // Begin transaction - all test data will be rolled back
        _transaction = await _context.Database.BeginTransactionAsync();

        _flightRepository = new FlightRepositoryAsync(_context);
        _tripRepository = new TripRepositoryAsync(_context);
    }

    public async Task DisposeAsync()
    {
        // Rollback transaction to keep omniflow_dev clean
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByTripAsync_ReturnsFlightsSortedByDirectionAndDeparture()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return; // Skip if no user

        var trip = new Trip
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };
        await _tripRepository.AddAsync(trip);

        var flight1 = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(8),
            ArrivalAt = GetTestDateTime(8).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var flight2 = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(7),
            ArrivalAt = GetTestDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Pegasus",
            FlightNumber = "PC5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 350,
            TotalPrice = 350,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var flight3 = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Return,
            FromCity = "Antalya",
            FromAirport = "AYT",
            ToCity = "Istanbul",
            ToAirport = "IST",
            DepartureAt = GetTestDateTime(14),
            ArrivalAt = GetTestDateTime(14).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _flightRepository.AddAsync(flight1);
        await _flightRepository.AddAsync(flight2);
        await _flightRepository.AddAsync(flight3);

        // Act
        var result = await _flightRepository.GetByTripAsync(trip.Id);

        // Assert
        result.Should().HaveCount(3);
        // Should be sorted by FlightDirection (Outbound first), then DepartureAt
        result[0].FlightDirection.Should().Be(FlightDirection.Outbound);
        result[0].FlightNumber.Should().Be("PC5678"); // Earlier departure
        result[1].FlightDirection.Should().Be(FlightDirection.Outbound);
        result[1].FlightNumber.Should().Be("TK1234"); // Later departure
        result[2].FlightDirection.Should().Be(FlightDirection.Return);
    }

    [Fact]
    public async Task GetByTripAsync_WithDirectionFilter_ReturnsFilteredFlights()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };
        await _tripRepository.AddAsync(trip);

        var outboundFlight = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(7),
            ArrivalAt = GetTestDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var returnFlight = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Return,
            FromCity = "Antalya",
            FromAirport = "AYT",
            ToCity = "Istanbul",
            ToAirport = "IST",
            DepartureAt = GetTestDateTime(14),
            ArrivalAt = GetTestDateTime(14).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _flightRepository.AddAsync(outboundFlight);
        await _flightRepository.AddAsync(returnFlight);

        // Act
        var result = await _flightRepository.GetByTripAsync(trip.Id, FlightDirection.Outbound);

        // Assert
        result.Should().HaveCount(1);
        result[0].FlightDirection.Should().Be(FlightDirection.Outbound);
    }

    [Fact]
    public async Task GetByGroupAsync_ReturnsGroupedFlights()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };
        await _tripRepository.AddAsync(trip);

        var groupId = Guid.NewGuid();

        var outboundFlight = new Flight
        {
            TripId = trip.Id,
            ItineraryGroupId = groupId,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(7),
            ArrivalAt = GetTestDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var returnFlight = new Flight
        {
            TripId = trip.Id,
            ItineraryGroupId = groupId,
            FlightDirection = FlightDirection.Return,
            FromCity = "Antalya",
            FromAirport = "AYT",
            ToCity = "Istanbul",
            ToAirport = "IST",
            DepartureAt = GetTestDateTime(14),
            ArrivalAt = GetTestDateTime(14).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _flightRepository.AddAsync(outboundFlight);
        await _flightRepository.AddAsync(returnFlight);

        // Act
        var result = await _flightRepository.GetByGroupAsync(groupId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(f => f.FlightDirection == FlightDirection.Outbound);
        result.Should().Contain(f => f.FlightDirection == FlightDirection.Return);
    }

    [Fact]
    public async Task GetBookedFlightsByDirectionAsync_ReturnsOnlyBookedFlights()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };
        await _tripRepository.AddAsync(trip);

        var bookedFlight = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(7),
            ArrivalAt = GetTestDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            IsBooked = true,
            BookedAt = GetUtcDateTime(0),
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var unbookedFlight = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(8),
            ArrivalAt = GetTestDateTime(8).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Pegasus",
            FlightNumber = "PC5678",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 350,
            TotalPrice = 350,
            CurrencyCode = "USD",
            IsBooked = false,
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _flightRepository.AddAsync(bookedFlight);
        await _flightRepository.AddAsync(unbookedFlight);

        // Act
        var result = await _flightRepository.GetBookedFlightsByDirectionAsync(trip.Id, FlightDirection.Outbound);

        // Assert
        result.Should().HaveCount(1);
        result[0].IsBooked.Should().BeTrue();
        result[0].FlightNumber.Should().Be("TK1234");
    }

    [Fact]
    public async Task AddAsync_NewFlight_PersistsToDatabase()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            City = "Antalya",
            Country = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyle = TravelStyle.Adventure
        };
        await _tripRepository.AddAsync(trip);

        var flight = new Flight
        {
            TripId = trip.Id,
            FlightDirection = FlightDirection.Outbound,
            FromCity = "Istanbul",
            FromAirport = "IST",
            ToCity = "Antalya",
            ToAirport = "AYT",
            DepartureAt = GetTestDateTime(7),
            ArrivalAt = GetTestDateTime(7).AddHours(1.5),
            DurationMinutes = 90,
            Airline = "Turkish Airlines",
            FlightNumber = "TK1234",
            CabinClass = CabinClass.Economy,
            IsDirect = true,
            PricePerPerson = 500,
            TotalPrice = 500,
            CurrencyCode = "USD",
            Status = FlightStatus.Scheduled,
            DataSource = FlightDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        // Act
        var result = await _flightRepository.AddAsync(flight);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);

        var fromDb = await _flightRepository.GetByIdAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.FlightNumber.Should().Be("TK1234");
        fromDb.FromCity.Should().Be("Istanbul");
        fromDb.ToCity.Should().Be("Antalya");
    }
}