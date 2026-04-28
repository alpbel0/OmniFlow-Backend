using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface IRecommendationService
{
    /// <summary>
    /// Gets recommended places for a city based on user preferences,
    /// scored and grouped by visibility.
    /// </summary>
    /// <param name="city">Destination city</param>
    /// <param name="budgetTier">Budget tier (usually AdjustedBudgetTier ?? BudgetTier)</param>
    /// <param name="companion">Travel companion</param>
    /// <param name="travelStyles">Selected travel styles (max 3)</param>
    /// <param name="tempo">Travel tempo</param>
    /// <param name="excludedPlaceIds">Place IDs already on timeline (to exclude from recommendations)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<RecommendedPlacesResult> GetRecommendedPlacesAsync(
        string city,
        BudgetTier budgetTier,
        TravelCompanion companion,
        List<TravelStyle> travelStyles,
        Tempo tempo,
        List<Guid> excludedPlaceIds,
        CancellationToken cancellationToken = default);
}
