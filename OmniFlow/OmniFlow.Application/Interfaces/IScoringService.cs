using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface IScoringService
{
    int CalculateGroupScore(PlaceCategory category, TravelCompanion companion);
    int CalculateStyleScore(PlaceCategory category, TravelStyle style);
    int CalculateStyleScoreAverage(PlaceCategory category, List<TravelStyle> styles);
    int CalculateGoogleMatchBonus(List<string> googleTags, List<TravelStyle> selectedStyles);
    int CalculateFinalScore(
        PlaceCategory category,
        TravelCompanion companion,
        List<TravelStyle> styles,
        List<string> googleTags);

    List<ScoredPlaceResult> ScoreAndSortPlaces(
        List<Place> places,
        TravelCompanion companion,
        List<TravelStyle> styles);
}
