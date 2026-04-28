using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Phase2;

public class ScoringServiceTests
{
    private readonly ScoringService _scoringService = new();

    // ------------------------------------------------------------------
    // Smoke Tests — Dictionary Coverage
    // ------------------------------------------------------------------

    [Fact]
    public void GroupScoreTable_ContainsAllPlaceCategoryCompanionCombinations()
    {
        var categories = Enum.GetValues<PlaceCategory>();
        var companions = Enum.GetValues<TravelCompanion>();

        foreach (var category in categories)
        {
            foreach (var companion in companions)
            {
                var act = () => _scoringService.CalculateGroupScore(category, companion);
                act.Should().NotThrow("every enum combination should be safe");
            }
        }
    }

    [Fact]
    public void StyleScoreTable_ContainsAllPlaceCategoryStyleCombinations()
    {
        var categories = Enum.GetValues<PlaceCategory>();
        var styles = Enum.GetValues<TravelStyle>();

        foreach (var category in categories)
        {
            foreach (var style in styles)
            {
                var act = () => _scoringService.CalculateStyleScore(category, style);
                act.Should().NotThrow("every enum combination should be safe");
            }
        }
    }

    [Fact]
    public void GoogleTagMapping_ContainsAllTravelStyles()
    {
        var styles = Enum.GetValues<TravelStyle>();

        foreach (var style in styles)
        {
            var bonus = _scoringService.CalculateGoogleMatchBonus(
                new List<string> { "Dummy" },
                new List<TravelStyle> { style });

            // Should not throw; actual value depends on dummy tag matching
            bonus.Should().BeGreaterOrEqualTo(0);
        }
    }

    // ------------------------------------------------------------------
    // Critical Combination Tests
    // ------------------------------------------------------------------

    [Fact]
    public void CalculateGroupScore_BarFamily_ReturnsMinus20()
    {
        var score = _scoringService.CalculateGroupScore(PlaceCategory.Bar, TravelCompanion.Family);
        score.Should().Be(-20);
    }

    [Fact]
    public void CalculateGroupScore_BeachFriends_Returns20()
    {
        var score = _scoringService.CalculateGroupScore(PlaceCategory.Beach, TravelCompanion.Friends);
        score.Should().Be(20);
    }

    [Fact]
    public void CalculateGroupScore_ThemeParkSolo_Returns0()
    {
        var score = _scoringService.CalculateGroupScore(PlaceCategory.ThemePark, TravelCompanion.Solo);
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateGroupScore_MissingKey_Returns0()
    {
        // Lake is not in the PRD scoring table
        var score = _scoringService.CalculateGroupScore(PlaceCategory.Lake, TravelCompanion.Solo);
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateStyleScore_BeachRomantic_Returns20()
    {
        var score = _scoringService.CalculateStyleScore(PlaceCategory.Beach, TravelStyle.Romantic);
        score.Should().Be(20);
    }

    [Fact]
    public void CalculateStyleScore_BarBudget_ReturnsMinus10()
    {
        var score = _scoringService.CalculateStyleScore(PlaceCategory.Bar, TravelStyle.Budget);
        score.Should().Be(-10);
    }

    [Fact]
    public void CalculateStyleScoreAverage_BeachRomanticAndNature_Returns20()
    {
        var avg = _scoringService.CalculateStyleScoreAverage(
            PlaceCategory.Beach,
            new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Nature });

        avg.Should().Be(20); // (20 + 20) / 2 = 20
    }

    [Fact]
    public void CalculateStyleScoreAverage_ThreeStyles_ReturnsCorrectAverage()
    {
        // Park: Romantic=20, Cultural=0, Adventure=10
        var avg = _scoringService.CalculateStyleScoreAverage(
            PlaceCategory.Park,
            new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Cultural, TravelStyle.Adventure });

        avg.Should().Be(10); // (20 + 0 + 10) / 3 = 10
    }

    [Fact]
    public void CalculateStyleScoreAverage_EmptyList_Returns0()
    {
        var avg = _scoringService.CalculateStyleScoreAverage(PlaceCategory.Beach, new List<TravelStyle>());
        avg.Should().Be(0);
    }

    [Fact]
    public void CalculateStyleScoreAverage_NullList_Returns0()
    {
        var avg = _scoringService.CalculateStyleScoreAverage(PlaceCategory.Beach, null!);
        avg.Should().Be(0);
    }

    [Fact]
    public void CalculateGoogleMatchBonus_MatchingTag_Returns10()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string> { "Beach" },
            new List<TravelStyle> { TravelStyle.Romantic });

        bonus.Should().Be(10);
    }

    [Fact]
    public void CalculateGoogleMatchBonus_NoMatchingTag_Returns0()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string> { "Industrial" },
            new List<TravelStyle> { TravelStyle.Romantic });

        bonus.Should().Be(0);
    }

    [Fact]
    public void CalculateGoogleMatchBonus_MultipleStylesOneMatches_Returns10()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string> { "Shopping" },
            new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Shopping });

        bonus.Should().Be(10); // only Shopping matches
    }

    [Fact]
    public void CalculateGoogleMatchBonus_MultipleStylesMultipleMatches_Returns20()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string> { "Beach", "Nature" },
            new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Nature });

        bonus.Should().Be(20); // Romantic matches Beach (+10), Nature matches Nature (+10)
    }

    [Fact]
    public void CalculateGoogleMatchBonus_EmptyTags_Returns0()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string>(),
            new List<TravelStyle> { TravelStyle.Romantic });

        bonus.Should().Be(0);
    }

    [Fact]
    public void CalculateGoogleMatchBonus_EmptyStyles_Returns0()
    {
        var bonus = _scoringService.CalculateGoogleMatchBonus(
            new List<string> { "Beach" },
            new List<TravelStyle>());

        bonus.Should().Be(0);
    }

    [Fact]
    public void CalculateFinalScore_CompleteScenario_ReturnsCorrectSum()
    {
        // Beach + Friends + [Romantic, Nature] + GoogleTags [Beach]
        // group_score = 20
        // style_avg = (20 + 20) / 2 = 20
        // google_bonus = 20 (Romantic matches Beach +10, Nature matches Beach +10)
        // final = 20 + 20 + 20 = 60
        var final = _scoringService.CalculateFinalScore(
            PlaceCategory.Beach,
            TravelCompanion.Friends,
            new List<TravelStyle> { TravelStyle.Romantic, TravelStyle.Nature },
            new List<string> { "Beach" });

        final.Should().Be(60);
    }

    [Fact]
    public void CalculateFinalScore_NegativeScore_ReturnsNegative()
    {
        // Bar + Family + [Romantic] + GoogleTags []
        // group_score = -20
        // style_avg = -20
        // google_bonus = 0
        // final = -40
        var final = _scoringService.CalculateFinalScore(
            PlaceCategory.Bar,
            TravelCompanion.Family,
            new List<TravelStyle> { TravelStyle.Romantic },
            new List<string>());

        final.Should().Be(-40);
    }

    // ------------------------------------------------------------------
    // ScoreAndSortPlaces
    // ------------------------------------------------------------------

    [Fact]
    public void ScoreAndSortPlaces_SortsByFinalScoreDescending()
    {
        var places = new List<Place>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Beach A",
                Category = PlaceCategory.Beach,
                GoogleTags = new List<string> { "Beach" }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bar B",
                Category = PlaceCategory.Bar,
                GoogleTags = new List<string>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Museum C",
                Category = PlaceCategory.Museum,
                GoogleTags = new List<string>()
            }
        };

        var results = _scoringService.ScoreAndSortPlaces(
            places,
            TravelCompanion.Friends,
            new List<TravelStyle> { TravelStyle.Romantic });

        results.Should().HaveCount(3);
        results[0].Place.Category.Should().Be(PlaceCategory.Beach); // highest score
        results[1].Place.Category.Should().Be(PlaceCategory.Museum); // mid score
        results[2].Place.Category.Should().Be(PlaceCategory.Bar); // lowest/negative score
    }

    [Fact]
    public void ScoreAndSortPlaces_ReturnsCorrectScoreComponents()
    {
        var place = new Place
        {
            Id = Guid.NewGuid(),
            Name = "Beach X",
            Category = PlaceCategory.Beach,
            GoogleTags = new List<string> { "Beach" }
        };

        var results = _scoringService.ScoreAndSortPlaces(
            new List<Place> { place },
            TravelCompanion.Friends,
            new List<TravelStyle> { TravelStyle.Romantic });

        results.Should().HaveCount(1);
        results[0].GroupScore.Should().Be(20);
        results[0].StyleScoreAvg.Should().Be(20);
        results[0].GoogleMatchBonus.Should().Be(10);
        results[0].FinalScore.Should().Be(50);
    }
}
