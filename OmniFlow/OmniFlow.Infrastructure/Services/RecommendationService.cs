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
        TransportPreference transportPreference,
        List<Guid> excludedPlaceIds,
        double? hubLatitude = null,
        double? hubLongitude = null,
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

        // 3. Score
        var scoredResults = _scoringService.ScoreAndSortPlaces(
            places.ToList(),
            companion,
            travelStyles ?? new List<TravelStyle>());

        var rankedResults = scoredResults
            .Select(scored => new
            {
                Scored = scored,
                DistanceKm = TryCalculateDistanceKm(scored.Place, hubLatitude, hubLongitude),
                AdjustedScore = scored.FinalScore - CalculateDistancePenaltyKm(
                    TryCalculateDistanceKm(scored.Place, hubLatitude, hubLongitude),
                    transportPreference)
            })
            .OrderByDescending(x => x.AdjustedScore)
            .ThenBy(x => x.DistanceKm ?? double.MaxValue)
            .ThenBy(x => x.Scored.Place.Name)
            .ToList();

        // 4. Group into 3 visibility buckets
        var result = new RecommendedPlacesResult
        {
            DailyCapacity = _timelineService.GetDailyCapacity(tempo)
        };

        for (var index = 0; index < rankedResults.Count; index++)
        {
            var ranked = rankedResults[index];
            var scored = ranked.Scored;
            var response = _mapper.Map<ScoredPlaceResponse>(scored.Place);
            response.FinalScore = ranked.AdjustedScore;
            response.GroupScore = scored.GroupScore;
            response.StyleScoreAvg = scored.StyleScoreAvg;
            response.GoogleMatchBonus = scored.GoogleMatchBonus;

            switch (ResolveVisibilityBucket(index, rankedResults.Count))
            {
                case RecommendationVisibilityBucket.Recommended:
                    result.Recommended.Add(response);
                    break;
                case RecommendationVisibilityBucket.Neutral:
                    result.Neutral.Add(response);
                    break;
                default:
                    result.Other.Add(response);
                    break;
            }
        }

        return result;
    }

    private static RecommendationVisibilityBucket ResolveVisibilityBucket(int index, int totalCount)
    {
        if (totalCount <= 0)
            return RecommendationVisibilityBucket.Recommended;

        var percentile = (double)index / totalCount;

        if (percentile < 0.40)
            return RecommendationVisibilityBucket.Recommended;

        if (percentile < 0.75)
            return RecommendationVisibilityBucket.Neutral;

        return RecommendationVisibilityBucket.Other;
    }

    private static double? TryCalculateDistanceKm(Place place, double? hubLatitude, double? hubLongitude)
    {
        if (!hubLatitude.HasValue || !hubLongitude.HasValue)
            return null;

        if (Math.Abs(place.Latitude) < double.Epsilon && Math.Abs(place.Longitude) < double.Epsilon)
            return null;

        return CalculateDistanceKm(hubLatitude.Value, hubLongitude.Value, place.Latitude, place.Longitude);
    }

    private static int CalculateDistancePenaltyKm(double? distanceKm, TransportPreference transportPreference)
    {
        if (!distanceKm.HasValue)
            return 0;

        return transportPreference switch
        {
            TransportPreference.Walking => distanceKm.Value switch
            {
                <= 2 => 0,
                <= 5 => 8,
                <= 10 => 18,
                _ => 30
            },
            TransportPreference.PublicTransport => distanceKm.Value switch
            {
                <= 5 => 0,
                <= 10 => 5,
                <= 20 => 12,
                _ => 20
            },
            TransportPreference.CarRental => distanceKm.Value switch
            {
                <= 20 => 0,
                <= 50 => 4,
                <= 100 => 10,
                _ => 18
            },
            _ => 0
        };
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private enum RecommendationVisibilityBucket
    {
        Recommended,
        Neutral,
        Other
    }
}
