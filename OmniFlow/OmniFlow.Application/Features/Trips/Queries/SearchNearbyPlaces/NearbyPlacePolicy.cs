using OmniFlow.Domain.Enums;
using System.Text.Json.Serialization;

namespace OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NearbyPlaceCategoryGroup
{
    All,
    FoodDrink,
    Sightseeing,
    Nature,
    Shopping
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NearbyPersonalizationTier
{
    Recommended,
    Neutral,
    Other
}

public sealed record NearbyPlaceRankingCandidate(
    Guid PlaceId,
    string Name,
    int PersonalizationScore,
    int DistanceMeters,
    int PreviousVisitCount);

public sealed record RankedNearbyPlace(
    Guid PlaceId,
    string Name,
    int PersonalizationScore,
    int DistanceMeters,
    int PreviousVisitCount,
    NearbyPersonalizationTier PersonalizationTier);

public static class NearbyPlacePolicy
{
    private const double EarthRadiusMeters = 6_371_000;

    private static readonly IReadOnlyDictionary<NearbyPlaceCategoryGroup, PlaceCategory[]> CategoryGroups =
        new Dictionary<NearbyPlaceCategoryGroup, PlaceCategory[]>
        {
            [NearbyPlaceCategoryGroup.FoodDrink] =
            [
                PlaceCategory.Restaurant, PlaceCategory.Cafe, PlaceCategory.Bar
            ],
            [NearbyPlaceCategoryGroup.Sightseeing] =
            [
                PlaceCategory.Museum, PlaceCategory.Historical, PlaceCategory.Church,
                PlaceCategory.Monument, PlaceCategory.Castle, PlaceCategory.Theater,
                PlaceCategory.Gallery, PlaceCategory.Memorial, PlaceCategory.Bridge,
                PlaceCategory.Tower, PlaceCategory.Attraction, PlaceCategory.Viewpoint,
                PlaceCategory.Information, PlaceCategory.Zoo, PlaceCategory.ThemePark,
                PlaceCategory.Aquarium, PlaceCategory.Entertainment
            ],
            [NearbyPlaceCategoryGroup.Nature] =
            [
                PlaceCategory.Park, PlaceCategory.Beach, PlaceCategory.Lake,
                PlaceCategory.Waterfall, PlaceCategory.Mountain, PlaceCategory.Forest,
                PlaceCategory.Cave, PlaceCategory.Nature
            ],
            [NearbyPlaceCategoryGroup.Shopping] =
            [
                PlaceCategory.Shopping, PlaceCategory.Supermarket, PlaceCategory.Mall,
                PlaceCategory.Market
            ]
        };

    public static IReadOnlyCollection<PlaceCategory> GetCategories(NearbyPlaceCategoryGroup group)
    {
        if (group == NearbyPlaceCategoryGroup.All)
            return CategoryGroups.Values.SelectMany(categories => categories).Distinct().ToArray();

        return CategoryGroups.TryGetValue(group, out var categories)
            ? categories
            : Array.Empty<PlaceCategory>();
    }

    public static int CalculateDistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        var longitudeDelta = DegreesToRadians(longitude2 - longitude1);
        var firstLatitude = DegreesToRadians(latitude1);
        var secondLatitude = DegreesToRadians(latitude2);
        var haversine =
            Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2) +
            Math.Cos(firstLatitude) * Math.Cos(secondLatitude) *
            Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);
        var angularDistance = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
        return (int)Math.Round(EarthRadiusMeters * angularDistance, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<RankedNearbyPlace> Rank(IReadOnlyCollection<NearbyPlaceRankingCandidate> candidates)
    {
        var scoreOrder = candidates
            .OrderByDescending(candidate => candidate.PersonalizationScore)
            .ThenBy(candidate => candidate.DistanceMeters)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.PlaceId)
            .ToArray();

        var tierByPlaceId = scoreOrder
            .Select((candidate, index) => new { candidate.PlaceId, Tier = ResolveTier(index, scoreOrder.Length) })
            .ToDictionary(item => item.PlaceId, item => item.Tier);

        return scoreOrder
            .OrderBy(candidate => candidate.PreviousVisitCount > 0)
            .ThenByDescending(candidate => candidate.PersonalizationScore)
            .ThenBy(candidate => candidate.DistanceMeters)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.PlaceId)
            .Select(candidate => new RankedNearbyPlace(
                candidate.PlaceId,
                candidate.Name,
                candidate.PersonalizationScore,
                candidate.DistanceMeters,
                candidate.PreviousVisitCount,
                tierByPlaceId[candidate.PlaceId]))
            .ToArray();
    }

    private static NearbyPersonalizationTier ResolveTier(int index, int totalCount)
    {
        if (totalCount == 0)
            return NearbyPersonalizationTier.Recommended;

        var percentile = (double)index / totalCount;
        if (percentile < 0.40)
            return NearbyPersonalizationTier.Recommended;
        return percentile < 0.75
            ? NearbyPersonalizationTier.Neutral
            : NearbyPersonalizationTier.Other;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
