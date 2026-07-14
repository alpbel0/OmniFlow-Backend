using FluentAssertions;
using OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public sealed class NearbyPlacePolicyTests
{
    [Fact]
    public void GetCategories_FoodDrink_ReturnsOnlyFoodAndDrinkCategories()
    {
        var categories = NearbyPlacePolicy.GetCategories(NearbyPlaceCategoryGroup.FoodDrink);

        categories.Should().BeEquivalentTo(new[]
        {
            PlaceCategory.Restaurant,
            PlaceCategory.Cafe,
            PlaceCategory.Bar
        });
    }

    [Fact]
    public void GetCategories_All_ExcludesHotelAndTransport()
    {
        var categories = NearbyPlacePolicy.GetCategories(NearbyPlaceCategoryGroup.All);

        categories.Should().NotContain(PlaceCategory.Hotel);
        categories.Should().NotContain(PlaceCategory.Transport);
        categories.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CalculateDistanceMeters_ForOneDegreeAtEquator_ReturnsGeodesicDistance()
    {
        var distance = NearbyPlacePolicy.CalculateDistanceMeters(0, 0, 0, 1);

        distance.Should().BeInRange(111_190, 111_200);
    }

    [Fact]
    public void Rank_PrioritizesDiscoveriesThenScoreDistanceNameAndId()
    {
        var closerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var fartherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var visitedId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var candidates = new[]
        {
            new NearbyPlaceRankingCandidate(visitedId, "Visited", 100, 10, 2),
            new NearbyPlaceRankingCandidate(fartherId, "Beta", 30, 200, 0),
            new NearbyPlaceRankingCandidate(closerId, "Alpha", 30, 100, 0)
        };

        var ranked = NearbyPlacePolicy.Rank(candidates);

        ranked.Select(item => item.PlaceId).Should().ContainInOrder(closerId, fartherId, visitedId);
        ranked[0].PersonalizationTier.Should().Be(NearbyPersonalizationTier.Recommended);
        ranked[2].PersonalizationTier.Should().Be(NearbyPersonalizationTier.Recommended);
    }

    [Fact]
    public void Rank_AssignsFortyThirtyFiveTwentyFivePercentileTiers()
    {
        var candidates = Enumerable.Range(0, 20)
            .Select(index => new NearbyPlaceRankingCandidate(
                Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}"),
                $"Place {index:D2}",
                100 - index,
                index,
                0))
            .ToArray();

        var ranked = NearbyPlacePolicy.Rank(candidates);

        ranked.Count(item => item.PersonalizationTier == NearbyPersonalizationTier.Recommended).Should().Be(8);
        ranked.Count(item => item.PersonalizationTier == NearbyPersonalizationTier.Neutral).Should().Be(7);
        ranked.Count(item => item.PersonalizationTier == NearbyPersonalizationTier.Other).Should().Be(5);
    }
}
