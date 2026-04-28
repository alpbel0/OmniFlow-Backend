using AutoMapper;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IPlaceRepositoryAsync _placeRepository;
    private readonly IScoringService _scoringService;
    private readonly ITimelineService _timelineService;
    private readonly IMapper _mapper;

    public RecommendationService(
        IPlaceRepositoryAsync placeRepository,
        IScoringService scoringService,
        ITimelineService timelineService,
        IMapper mapper)
    {
        _placeRepository = placeRepository;
        _scoringService = scoringService;
        _timelineService = timelineService;
        _mapper = mapper;
    }

    public async Task<RecommendedPlacesResult> GetRecommendedPlacesAsync(
        string city,
        BudgetTier budgetTier,
        TravelCompanion companion,
        List<TravelStyle> travelStyles,
        Tempo tempo,
        List<Guid> excludedPlaceIds,
        CancellationToken cancellationToken = default)
    {
        // 1. Fetch places from DB filtered by city and budget tier
        var places = await _placeRepository.GetByCityAndBudgetTierAsync(city, budgetTier);

        // 2. Exclude already-added places
        if (excludedPlaceIds?.Count > 0)
        {
            places = places
                .Where(p => !excludedPlaceIds.Contains(p.Id))
                .ToList();
        }

        // 3. Score and sort
        var scoredResults = _scoringService.ScoreAndSortPlaces(
            places.ToList(),
            companion,
            travelStyles ?? new List<TravelStyle>());

        // 4. Group into 3 visibility buckets
        var result = new RecommendedPlacesResult
        {
            DailyCapacity = _timelineService.GetDailyCapacity(tempo)
        };

        foreach (var scored in scoredResults)
        {
            var response = _mapper.Map<ScoredPlaceResponse>(scored.Place);
            response.FinalScore = scored.FinalScore;
            response.GroupScore = scored.GroupScore;
            response.StyleScoreAvg = scored.StyleScoreAvg;
            response.GoogleMatchBonus = scored.GoogleMatchBonus;

            if (scored.FinalScore > 0)
                result.Recommended.Add(response);
            else if (scored.FinalScore == 0)
                result.Neutral.Add(response);
            else
                result.Other.Add(response);
        }

        // Other: sort by descending (least negative first)
        result.Other = result.Other
            .OrderByDescending(r => r.FinalScore)
            .ToList();

        return result;
    }
}
