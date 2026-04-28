using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface IBudgetCalculationService
{
    decimal GetSeasonMultiplier(DateOnly date);

    (decimal EconomyThreshold, decimal StandardThreshold) SegmentHotel(string city);

    decimal CalculateFlightCost(Guid providerFlightId, int personCount, DateOnly travelDate);

    decimal CalculateHotelCost(Guid providerHotelId, int personCount, int nightCount, DateOnly checkInDate);

    Task<BudgetFallbackResult> CalculateBudgetFallbackAsync(
        decimal? manualBudget,
        BudgetTier selectedTier,
        string origin,
        List<TripDestination> destinations,
        int personCount);
}
