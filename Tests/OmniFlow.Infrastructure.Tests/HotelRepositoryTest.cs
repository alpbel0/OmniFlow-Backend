using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Tests;

public class HotelRepositoryTest : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private HotelRepositoryAsync _hotelRepository = null!;
    private TripRepositoryAsync _tripRepository = null!;
    private IDbContextTransaction _transaction = null!;

    private readonly string _connectionString = "Host=localhost;Port=5432;Database=omniflow_dev;Username=postgres;Password=postgres";

    private static DateTime GetTestDateTime(int daysToAdd)
    {
        // HotelConfiguration uses 'timestamp without time zone' for CheckIn and CheckOut
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

        _hotelRepository = new HotelRepositoryAsync(_context);
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
    public async Task GetByTripAsync_ReturnsHotelsOrderedByCheckIn()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return; // Skip if no user

        var trip = new Trip
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };
        await _tripRepository.AddAsync(trip);

        var hotel1 = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Hotel A",
            CheckIn = GetTestDateTime(10),
            CheckOut = GetTestDateTime(12),
            RoomType = RoomType.Double,
            PricePerNight = 100,
            TotalPrice = 200,
            CurrencyCode = "USD",
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var hotel2 = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Hotel B",
            CheckIn = GetTestDateTime(5), // Earlier check-in
            CheckOut = GetTestDateTime(7),
            RoomType = RoomType.Double,
            PricePerNight = 80,
            TotalPrice = 160,
            CurrencyCode = "USD",
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var hotel3 = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Hotel C",
            CheckIn = GetTestDateTime(7),
            CheckOut = GetTestDateTime(9),
            RoomType = RoomType.Suite,
            PricePerNight = 150,
            TotalPrice = 300,
            CurrencyCode = "USD",
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _hotelRepository.AddAsync(hotel1);
        await _hotelRepository.AddAsync(hotel2);
        await _hotelRepository.AddAsync(hotel3);

        // Act
        var result = await _hotelRepository.GetByTripAsync(trip.Id);

        // Assert
        result.Should().HaveCount(3);
        // Should be sorted by CheckIn ascending
        result[0].HotelName.Should().Be("Hotel B"); // CheckIn: Day 5
        result[1].HotelName.Should().Be("Hotel C"); // CheckIn: Day 7
        result[2].HotelName.Should().Be("Hotel A"); // CheckIn: Day 10
    }

    [Fact]
    public async Task GetByTripAsync_NoHotels_ReturnsEmptyList()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };
        await _tripRepository.AddAsync(trip);

        // Act
        var result = await _hotelRepository.GetByTripAsync(trip.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBookedHotelsByTripAsync_ReturnsOnlyBookedHotels()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };
        await _tripRepository.AddAsync(trip);

        var bookedHotel = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Booked Hotel",
            CheckIn = GetTestDateTime(7),
            CheckOut = GetTestDateTime(10),
            RoomType = RoomType.Suite,
            PricePerNight = 200,
            TotalPrice = 600,
            CurrencyCode = "USD",
            IsBooked = true,
            BookedAt = GetUtcDateTime(0),
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        var unbookedHotel = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Unbooked Hotel",
            CheckIn = GetTestDateTime(7),
            CheckOut = GetTestDateTime(10),
            RoomType = RoomType.Double,
            PricePerNight = 100,
            TotalPrice = 300,
            CurrencyCode = "USD",
            IsBooked = false,
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _hotelRepository.AddAsync(bookedHotel);
        await _hotelRepository.AddAsync(unbookedHotel);

        // Act
        var result = await _hotelRepository.GetBookedHotelsByTripAsync(trip.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].HotelName.Should().Be("Booked Hotel");
        result[0].IsBooked.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookedHotelsByTripAsync_NoBookedHotels_ReturnsEmptyList()
    {
        // Arrange - Get existing user
        var existingUser = await _context.Set<User>().FirstOrDefaultAsync();
        if (existingUser == null) return;

        var trip = new Trip
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            OwnerId = existingUser.Id,
            Status = TripStatus.Draft,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };
        await _tripRepository.AddAsync(trip);

        var unbookedHotel = new Hotel
        {
            TripId = trip.Id,
            HotelName = "Unbooked Hotel",
            CheckIn = GetTestDateTime(7),
            CheckOut = GetTestDateTime(10),
            RoomType = RoomType.Double,
            PricePerNight = 100,
            TotalPrice = 300,
            CurrencyCode = "USD",
            IsBooked = false,
            Status = HotelStatus.Confirmed,
            DataSource = HotelDataSource.Mock,
            DataFetchedAt = GetUtcDateTime(0)
        };

        await _hotelRepository.AddAsync(unbookedHotel);

        // Act
        var result = await _hotelRepository.GetBookedHotelsByTripAsync(trip.Id);

        // Assert
        result.Should().BeEmpty();
    }
}