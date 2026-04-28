using Microsoft.Extensions.Caching.Memory;
using Moq;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Phase2;

public class BudgetCalculationServiceTests
{
    private readonly Mock<IProviderFlightRepositoryAsync> _flightRepoMock = new();
    private readonly Mock<IProviderHotelRepositoryAsync> _hotelRepoMock = new();

    private BudgetCalculationService CreateService()
    {
        return new BudgetCalculationService(
            _flightRepoMock.Object,
            _hotelRepoMock.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    // ------------------------------------------------------------------
    // GetSeasonMultiplier
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(8, 1.5)]   // Yaz
    [InlineData(7, 1.5)]   // Yaz
    [InlineData(12, 1.2)]  // Kış
    [InlineData(1, 1.2)]   // Kış
    [InlineData(4, 1.1)]   // İlkbahar
    [InlineData(9, 1.0)]   // Sonbahar
    [InlineData(10, 1.0)]  // Sonbahar
    public void GetSeasonMultiplier_ReturnsCorrectValue(int month, decimal expected)
    {
        var service = CreateService();
        var result = service.GetSeasonMultiplier(new DateOnly(2026, month, 15));
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSeasonMultiplier_FallbackMonth_Returns1()
    {
        var service = CreateService();
        // November is explicitly 1.0 in the dictionary — test normal fallback
        var result = service.GetSeasonMultiplier(new DateOnly(2026, 11, 15));
        result.Should().Be(1.0m);
    }

    // ------------------------------------------------------------------
    // SegmentHotel
    // ------------------------------------------------------------------

    [Fact]
    public void SegmentHotel_WithPrices_ReturnsCorrectThresholds()
    {
        // 10 prices: 50, 80, 100, 120, 150, 180, 200, 250, 300, 500
        // Economy threshold = 20% → index 1 (0-based) → 80
        // Standard threshold = 90% → index 8 (0-based) → 300
        var prices = new List<decimal> { 50m, 80m, 100m, 120m, 150m, 180m, 200m, 250m, 300m, 500m };
        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Paris"))
            .ReturnsAsync(prices);

        var service = CreateService();
        var (economy, standard) = service.SegmentHotel("Paris");

        economy.Should().Be(80m);
        standard.Should().Be(300m);
    }

    [Fact]
    public void SegmentHotel_EmptyPrices_ReturnsZero()
    {
        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("EmptyCity"))
            .ReturnsAsync(new List<decimal>());

        var service = CreateService();
        var (economy, standard) = service.SegmentHotel("EmptyCity");

        economy.Should().Be(0m);
        standard.Should().Be(0m);
    }

    [Fact]
    public void SegmentHotel_CacheHit_DoesNotCallRepoAgain()
    {
        var prices = new List<decimal> { 100m, 200m, 300m };
        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("CachedCity"))
            .ReturnsAsync(prices);

        var service = CreateService();

        // First call
        service.SegmentHotel("CachedCity");
        // Second call
        service.SegmentHotel("CachedCity");

        _hotelRepoMock.Verify(r => r.GetDistinctPricesByCityAsync("CachedCity"), Times.Once);
    }

    [Theory]
    [MemberData(nameof(SegmentHotelData))]
    public void SegmentHotel_VariousSets_ReturnsExpectedThresholds(
        List<decimal> prices,
        decimal expectedEconomy,
        decimal expectedStandard)
    {
        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync(It.IsAny<string>()))
            .ReturnsAsync(prices);

        var service = CreateService();
        var (economy, standard) = service.SegmentHotel("TestCity");

        economy.Should().Be(expectedEconomy);
        standard.Should().Be(expectedStandard);
    }

    public static TheoryData<List<decimal>, decimal, decimal> SegmentHotelData()
    {
        return new TheoryData<List<decimal>, decimal, decimal>
        {
            // 5 prices: 100, 150, 200, 250, 300
            // economy idx = ceil(5*0.20)-1 = 0 → 100
            // standard idx = ceil(5*0.90)-1 = 4 → 300
            { new() { 100m, 150m, 200m, 250m, 300m }, 100m, 300m },

            // 1 price: 500
            // economy idx = 0 → 500
            // standard idx = 0 → 500
            { new() { 500m }, 500m, 500m },

            // 20 prices: 10, 20, 30, ..., 200
            // economy idx = ceil(20*0.20)-1 = 3 → 40
            // standard idx = ceil(20*0.90)-1 = 17 → 180
            { Enumerable.Range(1, 20).Select(i => i * 10m).ToList(), 40m, 180m },

            // Duplicates should be collapsed by Distinct
            { new() { 50m, 50m, 100m, 100m, 200m }, 50m, 200m },
        };
    }

    // ------------------------------------------------------------------
    // CalculateFlightCost
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateFlightCost_WithSeasonMultiplier_ReturnsCorrectTotal()
    {
        var flight = new ProviderFlight
        {
            Id = Guid.NewGuid(),
            Price = 200m,
            DepartureCity = "IST",
            ArrivalCity = "FCO"
        };

        _flightRepoMock.Setup(r => r.GetByIdAsync(flight.Id))
            .ReturnsAsync(flight);

        var service = CreateService();
        var cost = service.CalculateFlightCost(flight.Id, 3, new DateOnly(2026, 8, 10)); // August → 1.5

        cost.Should().Be(200m * 3 * 1.5m); // 900
    }

    [Fact]
    public void CalculateFlightCost_WithoutSeasonMultiplier_ReturnsCorrectTotal()
    {
        var flight = new ProviderFlight
        {
            Id = Guid.NewGuid(),
            Price = 200m,
            DepartureCity = "IST",
            ArrivalCity = "FCO"
        };

        _flightRepoMock.Setup(r => r.GetByIdAsync(flight.Id))
            .ReturnsAsync(flight);

        var service = CreateService();
        var cost = service.CalculateFlightCost(flight.Id, 2, new DateOnly(2026, 9, 15)); // September → 1.0

        cost.Should().Be(200m * 2 * 1.0m); // 400
    }

    [Fact]
    public void CalculateFlightCost_FlightNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _flightRepoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((ProviderFlight?)null);

        var service = CreateService();
        var act = () => service.CalculateFlightCost(id, 1, new DateOnly(2026, 8, 10));

        act.Should().Throw<OmniFlow.Application.Exceptions.EntityNotFoundException>()
            .WithMessage($"*ProviderFlight with id '{id}' was not found*");
    }

    // ------------------------------------------------------------------
    // CalculateHotelCost
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateHotelCost_WithSeasonMultiplier_ReturnsCorrectTotal()
    {
        var hotel = new ProviderHotel
        {
            Id = Guid.NewGuid(),
            PricePerNight = 150m,
            City = "Paris"
        };

        _hotelRepoMock.Setup(r => r.GetByIdAsync(hotel.Id))
            .ReturnsAsync(hotel);

        var service = CreateService();
        var cost = service.CalculateHotelCost(hotel.Id, 2, 5, new DateOnly(2026, 12, 1)); // December → 1.2

        cost.Should().Be(150m * 2 * 5 * 1.2m); // 1800
    }

    [Fact]
    public void CalculateHotelCost_WithoutSeasonMultiplier_ReturnsCorrectTotal()
    {
        var hotel = new ProviderHotel
        {
            Id = Guid.NewGuid(),
            PricePerNight = 100m,
            City = "Rome"
        };

        _hotelRepoMock.Setup(r => r.GetByIdAsync(hotel.Id))
            .ReturnsAsync(hotel);

        var service = CreateService();
        var cost = service.CalculateHotelCost(hotel.Id, 1, 3, new DateOnly(2026, 10, 10)); // October → 1.0

        cost.Should().Be(100m * 1 * 3 * 1.0m); // 300
    }

    [Fact]
    public void CalculateHotelCost_HotelNotFound_Throws()
    {
        var id = Guid.NewGuid();
        _hotelRepoMock.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((ProviderHotel?)null);

        var service = CreateService();
        var act = () => service.CalculateHotelCost(id, 1, 1, new DateOnly(2026, 8, 10));

        act.Should().Throw<OmniFlow.Application.Exceptions.EntityNotFoundException>()
            .WithMessage($"*ProviderHotel with id '{id}' was not found*");
    }

    // ------------------------------------------------------------------
    // CalculateBudgetFallbackAsync
    // ------------------------------------------------------------------

    private static List<TripDestination> CreateTestDestination(string city, string country, int orderIndex, string arrival, string departure)
    {
        var dest = new TripDestination(
            DateOnly.Parse(arrival),
            DateOnly.Parse(departure),
            city, country, orderIndex);
        return new List<TripDestination> { dest };
    }

    private static List<ProviderHotel> CreateTestHotels()
    {
        // 10 distinct prices → Economy threshold = 80 (20%), Standard threshold = 800 (90%)
        return new List<ProviderHotel>
        {
            new() { PricePerNight = 50m },    // Economy (≤80)
            new() { PricePerNight = 80m },    // Economy (≤80)
            new() { PricePerNight = 100m },   // Standard (>80, ≤800)
            new() { PricePerNight = 150m },   // Standard
            new() { PricePerNight = 200m },   // Standard
            new() { PricePerNight = 300m },   // Standard
            new() { PricePerNight = 400m },   // Standard
            new() { PricePerNight = 600m },   // Standard
            new() { PricePerNight = 800m },   // Standard
            new() { PricePerNight = 1000m }   // Premium (>800)
        };
    }

    private void SetupParisMocks(decimal flightPrice = 200m)
    {
        _flightRepoMock.Setup(r => r.GetByRouteAsync("Istanbul", "Paris", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = flightPrice } });
        _flightRepoMock.Setup(r => r.GetByRouteAsync("Paris", "Istanbul", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = flightPrice } });

        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Paris"))
            .ReturnsAsync(new List<decimal> { 50m, 80m, 100m, 150m, 200m, 300m, 400m, 600m, 800m, 1000m });
        _hotelRepoMock.Setup(r => r.GetByCityAsync("Paris"))
            .ReturnsAsync(CreateTestHotels());
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_NullBudget_ReturnsNoAdjustment()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(null, BudgetTier.Premium, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Premium);
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_ZeroBudget_ReturnsNoAdjustment()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(0m, BudgetTier.Standard, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Standard);
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_SufficientForPremium_ReturnsNoAdjustment()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");
        // Premium: flights 200*2*2 = 800, hotel 1000*2*3 = 6000 → total 6800
        SetupParisMocks(200m);

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(7000m, BudgetTier.Premium, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Premium);
        result.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_PremiumToStandard_ReturnsStandardWithMessage()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");
        // Premium total = 6800, Standard total = 1400 (flights 800 + hotel 100*2*3 = 600)
        SetupParisMocks(200m);

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(2000m, BudgetTier.Premium, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeTrue();
        result.AdjustedTier.Should().Be(BudgetTier.Standard);
        result.Messages.Should().Contain("Bütçenize göre otel tercihiniz Standard olarak güncellendi.");
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_PremiumToEconomy_ReturnsEconomyWithCascadeMessage()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");
        // Premium total = 6800, Standard total = 1400, Economy total = 1100 (flights 800 + hotel 50*2*3 = 300)
        // Budget = 1200 → Economy yeter (1100), Standard yetmez (1400)
        SetupParisMocks(200m);

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(1200m, BudgetTier.Premium, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeTrue();
        result.AdjustedTier.Should().Be(BudgetTier.Economy);
        result.Messages.Should().Contain("Bütçeniz Premium ve Standard için yetersiz olduğundan tercihiniz Economy olarak ayarlandı.");
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_StandardToEconomy_ReturnsEconomyWithMessage()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");
        // Standard total = 1400, Economy total = 1100
        // Budget = 1200 → Economy yeter, Standard yetmez
        SetupParisMocks(200m);

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(1200m, BudgetTier.Standard, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeTrue();
        result.AdjustedTier.Should().Be(BudgetTier.Economy);
        result.Messages.Should().Contain("Bütçenize göre otel tercihiniz Economy olarak güncellendi.");
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_EconomyInsufficient_ReturnsEconomyWithWarning()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");
        // Economy total = 1100
        // Budget = 500 → Economy bile yetmez
        SetupParisMocks(200m);

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(500m, BudgetTier.Economy, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeTrue();
        result.AdjustedTier.Should().Be(BudgetTier.Economy);
        result.Messages.Should().Contain(m => m.Contains("yetersiz görünüyor"));
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_MultipleDestinations_CalculatesCorrectly()
    {
        var destinations = new List<TripDestination>
        {
            new(DateOnly.Parse("2026-09-10"), DateOnly.Parse("2026-09-13"), "Paris", "France", 1),
            new(DateOnly.Parse("2026-09-13"), DateOnly.Parse("2026-09-15"), "Rome", "Italy", 2)
        };

        _flightRepoMock.Setup(r => r.GetByRouteAsync("Istanbul", "Paris", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = 200m } });
        _flightRepoMock.Setup(r => r.GetByRouteAsync("Paris", "Rome", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = 200m } });
        _flightRepoMock.Setup(r => r.GetByRouteAsync("Rome", "Istanbul", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = 200m } });

        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Paris"))
            .ReturnsAsync(new List<decimal> { 50m, 100m, 200m });
        _hotelRepoMock.Setup(r => r.GetByCityAsync("Paris"))
            .ReturnsAsync(new List<ProviderHotel>
            {
                new() { PricePerNight = 50m },
                new() { PricePerNight = 100m },
                new() { PricePerNight = 200m }
            });

        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Rome"))
            .ReturnsAsync(new List<decimal> { 50m, 100m, 200m });
        _hotelRepoMock.Setup(r => r.GetByCityAsync("Rome"))
            .ReturnsAsync(new List<ProviderHotel>
            {
                new() { PricePerNight = 50m },
                new() { PricePerNight = 100m },
                new() { PricePerNight = 200m }
            });

        // Standard total: flights 200*2*3 = 1200, Paris hotel 100*2*3 = 600, Rome hotel 100*2*2 = 400 → 2200
        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(2500m, BudgetTier.Standard, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Standard);
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_MissingFlight_ReturnsEstimatedPriceAndWarning()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");

        _flightRepoMock.Setup(r => r.GetByRouteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight>());

        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Paris"))
            .ReturnsAsync(new List<decimal> { 50m, 100m, 200m });
        _hotelRepoMock.Setup(r => r.GetByCityAsync("Paris"))
            .ReturnsAsync(new List<ProviderHotel> { new() { PricePerNight = 100m } });

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(2000m, BudgetTier.Standard, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Standard);
        result.Messages.Should().Contain(m => m.Contains("tahmini ulaşım maliyetleri"));
    }

    [Fact]
    public async Task CalculateBudgetFallbackAsync_MissingHotel_ReturnsEstimatedPriceAndWarning()
    {
        var destinations = CreateTestDestination("Paris", "France", 1, "2026-09-10", "2026-09-13");

        _flightRepoMock.Setup(r => r.GetByRouteAsync("Istanbul", "Paris", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = 200m } });
        _flightRepoMock.Setup(r => r.GetByRouteAsync("Paris", "Istanbul", It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ProviderFlight> { new() { Price = 200m } });

        _hotelRepoMock.Setup(r => r.GetDistinctPricesByCityAsync("Paris"))
            .ReturnsAsync(new List<decimal>());
        _hotelRepoMock.Setup(r => r.GetByCityAsync("Paris"))
            .ReturnsAsync(new List<ProviderHotel>());

        var service = CreateService();
        var result = await service.CalculateBudgetFallbackAsync(2000m, BudgetTier.Standard, "Istanbul", destinations, 2);

        result.IsAdjusted.Should().BeFalse();
        result.AdjustedTier.Should().Be(BudgetTier.Standard);
        result.Messages.Should().Contain(m => m.Contains("tahmini konaklama maliyetleri"));
    }
}
