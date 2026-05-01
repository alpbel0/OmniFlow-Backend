using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Models;

namespace OmniFlow.Api.IntegrationTests.Setup;

public static class TestDatabaseSeeder
{
    public const string TestUserEmail = "testuser@omniflow.com";
    public const string TestUserPassword = "TestUser123!";
    public const string TestUserUsername = "testuser";

    public const string AdminEmail = "admin@omniflow.com";
    public const string AdminPassword = "Admin123!";
    public static readonly DateOnly ProviderSeedDate = new(2030, 1, 15);

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();

        // Seed roles
        foreach (var role in new[] { Roles.Traveler.ToString(), Roles.Admin.ToString() })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Seed test traveler user
        if (await userManager.FindByEmailAsync(TestUserEmail) is null)
        {
            var appUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = TestUserUsername,
                Email = TestUserEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(appUser, TestUserPassword);
            await userManager.AddToRoleAsync(appUser, Roles.Traveler.ToString());

            dbContext.Users.Add(new User
            {
                Id = appUser.Id,
                Username = TestUserUsername,
                Email = TestUserEmail,
                Role = Roles.Traveler,
                IsVerified = false
            });
            await dbContext.SaveChangesAsync();
        }

        // Seed admin user
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = AdminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(adminUser, AdminPassword);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());

            dbContext.Users.Add(new User
            {
                Id = adminUser.Id,
                Username = "admin",
                Email = AdminEmail,
                Role = Roles.Admin,
                IsVerified = true
            });
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Clears feed-related tables for test isolation.
    /// Use before tests that depend on specific feed state.
    /// </summary>
    public static async Task ClearFeedDataAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();

        // Clear in correct order to avoid foreign key violations
        dbContext.CommentUpvotes.RemoveRange(dbContext.CommentUpvotes);
        dbContext.PostUpvotes.RemoveRange(dbContext.PostUpvotes);
        dbContext.Comments.RemoveRange(dbContext.Comments);
        dbContext.Posts.RemoveRange(dbContext.Posts);
        dbContext.Follows.RemoveRange(dbContext.Follows);

        await dbContext.SaveChangesAsync();
    }

    public static async Task CleanRefreshTokensAsync(IServiceProvider serviceProvider, Guid userId)
    {
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();
        var tokens = dbContext.RefreshTokens.Where(t => t.UserId == userId).ToList();
        dbContext.RefreshTokens.RemoveRange(tokens);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds provider flight and hotel data for provider controller integration tests.
    /// Idempotent: upserts deterministic test records by fixed IDs.
    /// </summary>
    public static async Task SeedProviderDataAsync(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();

        var now = DateTime.UtcNow;
        var baseDate = ProviderSeedDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Unspecified);

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            FlightNumber = "TK1001",
            Airline = "Turkish Airlines",
            DepartureCity = "Istanbul",
            ArrivalCity = "Paris",
            DepartureAirportCode = "IST",
            ArrivalAirportCode = "CDG",
            DepartureTime = baseDate.AddDays(1),
            ArrivalTime = baseDate.AddDays(1).AddHours(3).AddMinutes(30),
            DurationMinutes = 210,
            Price = 200,
            CurrencyCode = "USD",
            AvailableSeats = 50,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
            FlightNumber = "AF1201",
            Airline = "Air France",
            DepartureCity = "Paris",
            ArrivalCity = "Rome",
            DepartureAirportCode = "CDG",
            ArrivalAirportCode = "FCO",
            DepartureTime = baseDate.AddDays(2),
            ArrivalTime = baseDate.AddDays(2).AddHours(2).AddMinutes(15),
            DurationMinutes = 135,
            Price = 150,
            CurrencyCode = "USD",
            AvailableSeats = 40,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a3333333-3333-3333-3333-333333333333"),
            FlightNumber = "AZ8001",
            Airline = "ITA Airways",
            DepartureCity = "Rome",
            ArrivalCity = "Berlin",
            DepartureAirportCode = "FCO",
            ArrivalAirportCode = "BER",
            DepartureTime = baseDate.AddDays(3),
            ArrivalTime = baseDate.AddDays(3).AddHours(2),
            DurationMinutes = 120,
            Price = 180,
            CurrencyCode = "USD",
            AvailableSeats = 60,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a4444444-4444-4444-4444-444444444444"),
            FlightNumber = "LH2001",
            Airline = "Lufthansa",
            DepartureCity = "Berlin",
            ArrivalCity = "Istanbul",
            DepartureAirportCode = "BER",
            ArrivalAirportCode = "IST",
            DepartureTime = baseDate.AddDays(4),
            ArrivalTime = baseDate.AddDays(4).AddHours(2).AddMinutes(45),
            DurationMinutes = 165,
            Price = 220,
            CurrencyCode = "USD",
            AvailableSeats = 45,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a5555555-5555-5555-5555-555555555555"),
            FlightNumber = "TK2002",
            Airline = "Turkish Airlines",
            DepartureCity = "Paris",
            ArrivalCity = "Istanbul",
            DepartureAirportCode = "CDG",
            ArrivalAirportCode = "IST",
            DepartureTime = baseDate.AddDays(4),
            ArrivalTime = baseDate.AddDays(4).AddHours(3).AddMinutes(15),
            DurationMinutes = 195,
            Price = 250,
            CurrencyCode = "USD",
            AvailableSeats = 55,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a6666666-6666-6666-6666-666666666666"),
            FlightNumber = "TK2003",
            Airline = "Turkish Airlines",
            DepartureCity = "Paris",
            ArrivalCity = "Istanbul",
            DepartureAirportCode = "CDG",
            ArrivalAirportCode = "IST",
            DepartureTime = baseDate.AddDays(3),
            ArrivalTime = baseDate.AddDays(3).AddHours(3).AddMinutes(10),
            DurationMinutes = 190,
            Price = 245,
            CurrencyCode = "USD",
            AvailableSeats = 48,
            ProviderName = "MockProvider"
        });

        await UpsertProviderFlightAsync(dbContext, new ProviderFlight
        {
            Id = Guid.Parse("a7777777-7777-7777-7777-777777777777"),
            FlightNumber = "TK2004",
            Airline = "Turkish Airlines",
            DepartureCity = "Paris",
            ArrivalCity = "Istanbul",
            DepartureAirportCode = "CDG",
            ArrivalAirportCode = "IST",
            DepartureTime = baseDate.AddDays(5),
            ArrivalTime = baseDate.AddDays(5).AddHours(3).AddMinutes(20),
            DurationMinutes = 200,
            Price = 255,
            CurrencyCode = "USD",
            AvailableSeats = 52,
            ProviderName = "MockProvider"
        });

        await UpsertProviderHotelAsync(dbContext, new ProviderHotel
        {
            Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
            HotelName = "Budget Paris Inn",
            City = "Paris",
            Country = "France",
            Stars = 2,
            Rating = 3.5,
            ReviewCount = 120,
            ValidDate = DateOnly.FromDateTime(now),
            PricePerNight = 80,
            CurrencyCode = "USD",
            IsAvailable = true,
            ProviderName = "MockProvider"
        });

        await UpsertProviderHotelAsync(dbContext, new ProviderHotel
        {
            Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
            HotelName = "Standard Paris Hotel",
            City = "Paris",
            Country = "France",
            Stars = 3,
            Rating = 4.0,
            ReviewCount = 350,
            ValidDate = DateOnly.FromDateTime(now),
            PricePerNight = 150,
            CurrencyCode = "USD",
            IsAvailable = true,
            ProviderName = "MockProvider"
        });

        await UpsertProviderHotelAsync(dbContext, new ProviderHotel
        {
            Id = Guid.Parse("b3333333-3333-3333-3333-333333333333"),
            HotelName = "Premium Paris Palace",
            City = "Paris",
            Country = "France",
            Stars = 5,
            Rating = 4.8,
            ReviewCount = 890,
            ValidDate = DateOnly.FromDateTime(now),
            PricePerNight = 300,
            CurrencyCode = "USD",
            IsAvailable = true,
            ProviderName = "MockProvider"
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task UpsertProviderFlightAsync(IApplicationDbContext dbContext, ProviderFlight seedFlight)
    {
        var existingFlight = await dbContext.ProviderFlights.FindAsync(seedFlight.Id);
        if (existingFlight is null)
        {
            dbContext.ProviderFlights.Add(seedFlight);
            return;
        }

        existingFlight.FlightNumber = seedFlight.FlightNumber;
        existingFlight.Airline = seedFlight.Airline;
        existingFlight.AirlineLogoUrl = seedFlight.AirlineLogoUrl;
        existingFlight.DepartureCity = seedFlight.DepartureCity;
        existingFlight.ArrivalCity = seedFlight.ArrivalCity;
        existingFlight.DepartureAirportCode = seedFlight.DepartureAirportCode;
        existingFlight.ArrivalAirportCode = seedFlight.ArrivalAirportCode;
        existingFlight.DepartureTime = seedFlight.DepartureTime;
        existingFlight.ArrivalTime = seedFlight.ArrivalTime;
        existingFlight.DurationMinutes = seedFlight.DurationMinutes;
        existingFlight.Price = seedFlight.Price;
        existingFlight.CurrencyCode = seedFlight.CurrencyCode;
        existingFlight.AvailableSeats = seedFlight.AvailableSeats;
        existingFlight.ProviderName = seedFlight.ProviderName;
    }

    private static async Task UpsertProviderHotelAsync(IApplicationDbContext dbContext, ProviderHotel seedHotel)
    {
        var existingHotel = await dbContext.ProviderHotels.FindAsync(seedHotel.Id);
        if (existingHotel is null)
        {
            dbContext.ProviderHotels.Add(seedHotel);
            return;
        }

        existingHotel.HotelName = seedHotel.HotelName;
        existingHotel.City = seedHotel.City;
        existingHotel.Country = seedHotel.Country;
        existingHotel.Stars = seedHotel.Stars;
        existingHotel.Rating = seedHotel.Rating;
        existingHotel.ReviewCount = seedHotel.ReviewCount;
        existingHotel.ValidDate = seedHotel.ValidDate;
        existingHotel.PricePerNight = seedHotel.PricePerNight;
        existingHotel.CurrencyCode = seedHotel.CurrencyCode;
        existingHotel.IsAvailable = seedHotel.IsAvailable;
        existingHotel.ProviderName = seedHotel.ProviderName;
    }
}
