using Microsoft.Extensions.Caching.Memory;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Services;

public class BudgetCalculationService : IBudgetCalculationService
{
    private const decimal EstimatedFlightCost = 150m;
    private const decimal EstimatedHotelCostPerNight = 80m;

    private static readonly Dictionary<int, decimal> SeasonMultipliers = new()
    {
        { 12, 1.2m }, { 1, 1.2m }, { 2, 1.2m },   // Kış
        { 3, 1.1m },  { 4, 1.1m }, { 5, 1.1m },   // İlkbahar
        { 6, 1.5m },  { 7, 1.5m }, { 8, 1.5m },   // Yaz
        { 9, 1.0m },  { 10, 1.0m }, { 11, 1.0m }, // Sonbahar
    };

    private static readonly BudgetTier[] TierOrder = [BudgetTier.Premium, BudgetTier.Standard, BudgetTier.Economy];

    private readonly IProviderFlightRepositoryAsync _flightRepo;
    private readonly IProviderHotelRepositoryAsync _hotelRepo;
    private readonly IMemoryCache _cache;

    public BudgetCalculationService(
        IProviderFlightRepositoryAsync flightRepo,
        IProviderHotelRepositoryAsync hotelRepo,
        IMemoryCache cache)
    {
        _flightRepo = flightRepo;
        _hotelRepo = hotelRepo;
        _cache = cache;
    }

    public decimal GetSeasonMultiplier(DateOnly date)
    {
        return SeasonMultipliers.TryGetValue(date.Month, out var multiplier)
            ? multiplier
            : 1.0m;
    }

    public (decimal EconomyThreshold, decimal StandardThreshold) SegmentHotel(string city)
    {
        var cacheKey = $"hotel_segment_{city}";

        if (_cache.TryGetValue(cacheKey, out (decimal EconomyThreshold, decimal StandardThreshold) cached))
        {
            return cached;
        }

        var prices = _hotelRepo.GetDistinctPricesByCityAsync(city).GetAwaiter().GetResult();

        var result = CalculatePercentileThresholds(prices);

        _cache.Set(cacheKey, result, TimeSpan.FromHours(1));

        return result;
    }

    private static (decimal EconomyThreshold, decimal StandardThreshold) CalculatePercentileThresholds(IReadOnlyList<decimal> prices)
    {
        if (prices == null || prices.Count == 0)
            return (0m, 0m);

        var sorted = prices.OrderBy(p => p).ToList();
        var count = sorted.Count;

        var economyIndex = Math.Max(0, (int)Math.Ceiling(count * 0.20m) - 1);
        var standardIndex = Math.Max(0, (int)Math.Ceiling(count * 0.90m) - 1);

        var economyThreshold = sorted[economyIndex];
        var standardThreshold = sorted[standardIndex];

        return (economyThreshold, standardThreshold);
    }

    public decimal CalculateFlightCost(Guid providerFlightId, int personCount, DateOnly travelDate)
    {
        var flight = _flightRepo.GetByIdAsync(providerFlightId).GetAwaiter().GetResult();
        if (flight == null)
            throw new EntityNotFoundException(nameof(ProviderFlight), providerFlightId);

        return flight.Price * personCount * GetSeasonMultiplier(travelDate);
    }

    public decimal CalculateHotelCost(Guid providerHotelId, int personCount, int nightCount, DateOnly checkInDate)
    {
        var hotel = _hotelRepo.GetByIdAsync(providerHotelId).GetAwaiter().GetResult();
        if (hotel == null)
            throw new EntityNotFoundException(nameof(ProviderHotel), providerHotelId);

        return hotel.PricePerNight * personCount * nightCount * GetSeasonMultiplier(checkInDate);
    }

    public async Task<BudgetFallbackResult> CalculateBudgetFallbackAsync(
        decimal? manualBudget,
        BudgetTier selectedTier,
        string origin,
        List<TripDestination> destinations,
        int personCount)
    {
        // Step 1: No budget provided → exploration mode, no adjustment
        if (!manualBudget.HasValue || manualBudget.Value <= 0)
        {
            return new BudgetFallbackResult
            {
                OriginalTier = selectedTier,
                AdjustedTier = selectedTier,
                IsAdjusted = false,
                Messages = new List<string>()
            };
        }

        var sortedDestinations = destinations
            .OrderBy(d => d.OrderIndex)
            .ToList();

        var flightWarnings = new List<string>();
        var hotelWarnings = new List<string>();

        // Step 2: Cascade check — try selectedTier, then lower tiers
        var tiersToCheck = GetTiersToCheck(selectedTier);
        BudgetTier finalTier = BudgetTier.Economy;
        bool foundSufficient = false;

        foreach (var tier in tiersToCheck)
        {
            var totalCost = await CalculateTotalCostForTierAsync(
                tier, origin, sortedDestinations, personCount, flightWarnings, hotelWarnings);

            if (manualBudget.Value >= totalCost)
            {
                finalTier = tier;
                foundSufficient = true;
                break;
            }
        }

        // Step 3: Build messages
        var messages = BuildMessages(selectedTier, finalTier, foundSufficient);
        messages.AddRange(flightWarnings.Distinct());
        messages.AddRange(hotelWarnings.Distinct());

        return new BudgetFallbackResult
        {
            OriginalTier = selectedTier,
            AdjustedTier = finalTier,
            IsAdjusted = finalTier != selectedTier || !foundSufficient,
            Messages = messages
        };
    }

    private static List<BudgetTier> GetTiersToCheck(BudgetTier selectedTier)
    {
        var startIndex = Array.IndexOf(TierOrder, selectedTier);
        return TierOrder.Skip(startIndex).ToList();
    }

    private async Task<decimal> CalculateTotalCostForTierAsync(
        BudgetTier tier,
        string origin,
        List<TripDestination> destinations,
        int personCount,
        List<string> flightWarnings,
        List<string> hotelWarnings)
    {
        decimal total = 0;

        // --- Flights (legs + return) ---
        if (destinations.Count > 0)
        {
            // Origin -> First destination (arrival date)
            total += await CalculateLegFlightCostAsync(
                origin, destinations[0].City, destinations[0].ArrivalDate, personCount, flightWarnings);

            // Inter-destination flights
            for (int i = 0; i < destinations.Count - 1; i++)
            {
                total += await CalculateLegFlightCostAsync(
                    destinations[i].City,
                    destinations[i + 1].City,
                    destinations[i + 1].ArrivalDate,
                    personCount,
                    flightWarnings);
            }

            // Last destination -> Origin (departure date)
            total += await CalculateLegFlightCostAsync(
                destinations[^1].City, origin, destinations[^1].DepartureDate, personCount, flightWarnings);
        }

        // --- Hotels ---
        foreach (var dest in destinations)
        {
            total += await CalculateDestinationHotelCostAsync(dest, tier, personCount, hotelWarnings);
        }

        return total;
    }

    private async Task<decimal> CalculateLegFlightCostAsync(
        string fromCity, string toCity, DateOnly date, int personCount, List<string> warnings)
    {
        var flights = await _flightRepo.GetByRouteAsync(fromCity, toCity, date);
        if (flights.Any())
        {
            return flights.First().Price * personCount * GetSeasonMultiplier(date);
        }

        warnings.Add(
            $"Bazı rotalar ({fromCity} -> {toCity}) için kesin fiyat verisi bulunamadığından, " +
            "bütçe hesabında tahmini ulaşım maliyetleri kullanılmıştır. " +
            "İlerleyen adımlarda kendi bilet fiyatınızı girerek planı güncelleyebilirsiniz.");

        return EstimatedFlightCost * personCount * GetSeasonMultiplier(date);
    }

    private async Task<decimal> CalculateDestinationHotelCostAsync(
        TripDestination destination, BudgetTier tier, int personCount, List<string> warnings)
    {
        var cheapestPrice = await GetCheapestHotelPriceAsync(destination.City, tier);
        if (cheapestPrice.HasValue)
        {
            return cheapestPrice.Value * personCount * destination.NightCount * GetSeasonMultiplier(destination.ArrivalDate);
        }

        warnings.Add(
            $"{destination.City} şehri için seçtiğiniz kategoride otel verisi bulunamadığından, " +
            "bütçe hesabında tahmini konaklama maliyetleri kullanılmıştır.");

        return EstimatedHotelCostPerNight * personCount * destination.NightCount * GetSeasonMultiplier(destination.ArrivalDate);
    }

    private async Task<decimal?> GetCheapestHotelPriceAsync(string city, BudgetTier tier)
    {
        var thresholds = SegmentHotel(city);

        if (thresholds.EconomyThreshold == 0 && thresholds.StandardThreshold == 0)
            return null;

        var hotels = await _hotelRepo.GetByCityAsync(city);

        var filtered = tier switch
        {
            BudgetTier.Economy => hotels.Where(h => h.PricePerNight <= thresholds.EconomyThreshold),
            BudgetTier.Standard => hotels.Where(h =>
                h.PricePerNight > thresholds.EconomyThreshold &&
                h.PricePerNight <= thresholds.StandardThreshold),
            BudgetTier.Premium => hotels.Where(h => h.PricePerNight > thresholds.StandardThreshold),
            _ => hotels
        };

        return filtered.Any() ? filtered.Min(h => h.PricePerNight) : null;
    }

    private static List<string> BuildMessages(BudgetTier selectedTier, BudgetTier finalTier, bool foundSufficient)
    {
        var messages = new List<string>();

        if (!foundSufficient)
        {
            // No tier is sufficient — user is set to Economy (or already was)
            if (selectedTier != finalTier)
            {
                var skippedTiers = GetSkippedTierNames(selectedTier, finalTier);
                var tierNames = string.Join(" ve ", skippedTiers);
                messages.Add($"Bütçeniz {tierNames} için yetersiz olduğundan tercihiniz {finalTier} olarak ayarlandı.");
            }

            messages.Add(
                "Girdiğiniz bütçe, en uygun fiyatlı (Economy) tercihlerde bile bu seyahat planı için yetersiz görünüyor. " +
                "Lütfen bütçenizi güncellemeyi veya destinasyon sayısını azaltmayı düşünün.");
        }
        else if (finalTier != selectedTier)
        {
            // Found a sufficient tier, but had to downgrade
            var skippedTiers = GetSkippedTierNames(selectedTier, finalTier);
            if (skippedTiers.Count == 1)
            {
                messages.Add($"Bütçenize göre otel tercihiniz {finalTier} olarak güncellendi.");
            }
            else
            {
                var tierNames = string.Join(" ve ", skippedTiers);
                messages.Add($"Bütçeniz {tierNames} için yetersiz olduğundan tercihiniz {finalTier} olarak ayarlandı.");
            }
        }

        return messages;
    }

    private static List<string> GetSkippedTierNames(BudgetTier fromTier, BudgetTier toTier)
    {
        var fromIndex = Array.IndexOf(TierOrder, fromTier);
        var toIndex = Array.IndexOf(TierOrder, toTier);

        var skipped = new List<string>();
        for (int i = fromIndex; i < toIndex; i++)
        {
            skipped.Add(TierOrder[i].ToString());
        }
        return skipped;
    }
}
