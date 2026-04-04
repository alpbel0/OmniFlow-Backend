using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Parameters;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Tests;

public class StopRepositoryTest : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private StopRepositoryAsync _stopRepository = null!;
    private TripRepositoryAsync _tripRepository = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly string _connectionString = "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        _context = new ApplicationDbContext(options);

        // Begin transaction - all test data will be rolled back
        _transaction = await _context.Database.BeginTransactionAsync();

        _stopRepository = new StopRepositoryAsync(_context);
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
    public async Task GetByTripAsync_ReturnsStopsSortedByDayAndOrder()
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

        var stop1 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 3000.0,
            CustomName = "Stop 1 Day 1",
            CustomCategory = PlaceCategory.Restaurant
        };
        var stop2 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 1000.0,
            CustomName = "Stop 2 Day 1",
            CustomCategory = PlaceCategory.Museum
        };
        var stop3 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 2,
            OrderIndex = 1000.0,
            CustomName = "Stop 1 Day 2",
            CustomCategory = PlaceCategory.Cafe
        };

        await _stopRepository.AddAsync(stop1);
        await _stopRepository.AddAsync(stop2);
        await _stopRepository.AddAsync(stop3);

        // Act
        var result = await _stopRepository.GetByTripAsync(trip.Id);

        // Assert
        result.Should().HaveCount(3);
        // Should be sorted by DayNumber then OrderIndex
        result[0].CustomName.Should().Be("Stop 2 Day 1"); // Day 1, Order 1000
        result[1].CustomName.Should().Be("Stop 1 Day 1"); // Day 1, Order 3000
        result[2].CustomName.Should().Be("Stop 1 Day 2"); // Day 2
    }

    [Fact]
    public async Task GetByTripAndDayAsync_ReturnsStopsForSpecificDay()
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

        var stop1 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 1000.0,
            CustomName = "Day 1 Stop",
            CustomCategory = PlaceCategory.Restaurant
        };
        var stop2 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 2,
            OrderIndex = 1000.0,
            CustomName = "Day 2 Stop",
            CustomCategory = PlaceCategory.Museum
        };

        await _stopRepository.AddAsync(stop1);
        await _stopRepository.AddAsync(stop2);

        // Act
        var result = await _stopRepository.GetByTripAndDayAsync(trip.Id, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].CustomName.Should().Be("Day 1 Stop");
    }

    [Fact]
    public async Task GetByIdWithPlaceAsync_ReturnsStopWithPlaceNavigation()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var place = new Place
        {
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };
        _context.Set<Place>().Add(place);
        await _context.SaveChangesAsync();

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

        var stop = new Stop
        {
            TripId = trip.Id,
            PlaceId = place.Id,
            DayNumber = 1,
            OrderIndex = 1000.0
        };
        await _stopRepository.AddAsync(stop);

        // Act
        var result = await _stopRepository.GetByIdWithPlaceAsync(stop.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Place.Should().NotBeNull();
        result.Place!.Name.Should().Be("Test Place");
    }

    [Fact]
    public async Task GetLastStopInDayAsync_ReturnsStopWithHighestOrderIndex()
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

        var stop1 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 1000.0,
            CustomName = "First Stop",
            CustomCategory = PlaceCategory.Restaurant
        };
        var stop2 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 3000.0,
            CustomName = "Last Stop",
            CustomCategory = PlaceCategory.Museum
        };
        var stop3 = new Stop
        {
            TripId = trip.Id,
            DayNumber = 2,
            OrderIndex = 5000.0,
            CustomName = "Day 2 Stop",
            CustomCategory = PlaceCategory.Cafe
        };

        await _stopRepository.AddAsync(stop1);
        await _stopRepository.AddAsync(stop2);
        await _stopRepository.AddAsync(stop3);

        // Act
        var result = await _stopRepository.GetLastStopInDayAsync(trip.Id, 1);

        // Assert
        result.Should().NotBeNull();
        result!.CustomName.Should().Be("Last Stop");
        result.OrderIndex.Should().Be(3000.0);
    }

    [Fact]
    public async Task GetLastStopInDayAsync_NoStopsInDay_ReturnsNull()
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

        // Act
        var result = await _stopRepository.GetLastStopInDayAsync(trip.Id, 1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_NewStop_PersistsToDatabase()
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

        var stop = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 1000.0,
            CustomName = "New Stop",
            CustomCategory = PlaceCategory.Restaurant,
            ActivityPrice = 100,
            CurrencyCode = "USD",
            AddedBy = StopAddedBy.User
        };

        // Act
        var result = await _stopRepository.AddAsync(stop);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);

        var fromDb = await _stopRepository.GetByIdAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.CustomName.Should().Be("New Stop");
        fromDb.AddedBy.Should().Be(StopAddedBy.User);
    }

    [Fact]
    public async Task DeleteAsync_Stop_SoftDeletes()
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

        var stop = new Stop
        {
            TripId = trip.Id,
            DayNumber = 1,
            OrderIndex = 1000.0,
            CustomName = "Stop to Delete",
            CustomCategory = PlaceCategory.Restaurant
        };
        await _stopRepository.AddAsync(stop);

        // Act
        await _stopRepository.DeleteAsync(stop);

        // Assert - DeletedAt should be set (soft-delete)
        stop.DeletedAt.Should().NotBeNull();

        // Note: Due to Global Query Filter, GetByIdAsync won't find soft-deleted entities
        var fromDb = await _stopRepository.GetByIdAsync(stop.Id);
        fromDb.Should().BeNull();
    }
}