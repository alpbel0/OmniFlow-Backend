using AutoMapper;
using FluentAssertions;
using Moq;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Services;

namespace OmniFlow.UnitTests.Phase2;

public class RecommendationServiceTests
{
    private readonly Mock<IPlaceRepositoryAsync> _placeRepoMock = new();
    private readonly Mock<IScoringService> _scoringMock = new();
    private readonly Mock<ITimelineService> _timelineMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private RecommendationService CreateSut() =>
        new(_placeRepoMock.Object, _scoringMock.Object, _timelineMock.Object, _mapperMock.Object);

    #region GetRecommendedPlacesAsync

    [Fact]
    public async Task GetRecommendedPlaces_TwoRankedPlaces_SplitsAcrossRecommendedAndNeutral()
    {
        // Arrange
        var places = new List<Place>
        {
            CreatePlace(Guid.NewGuid(), "Colosseum", PlaceCategory.Historical),
            CreatePlace(Guid.NewGuid(), "Vatican", PlaceCategory.Museum),
        };

        var scored = new List<ScoredPlaceResult>
        {
            new() { Place = places[0], FinalScore = 30, GroupScore = 10, StyleScoreAvg = 10, GoogleMatchBonus = 10 },
            new() { Place = places[1], FinalScore = 25, GroupScore = 10, StyleScoreAvg = 5, GoogleMatchBonus = 10 },
        };

        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Standard))
            .ReturnsAsync(places);
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(places, TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(scored);
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Moderate)).Returns(5);

        SetupMapperForPlaces(places, scored);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle> { TravelStyle.Cultural }, Tempo.Moderate, TransportPreference.Walking, new List<Guid>());

        // Assert
        result.Recommended.Should().HaveCount(1);
        result.Neutral.Should().HaveCount(1);
        result.Other.Should().BeEmpty();
        result.DailyCapacity.Should().Be(5);
    }

    [Fact]
    public async Task GetRecommendedPlaces_FourRankedPlaces_SplitsAcrossThreeBucketsByPercentile()
    {
        // Arrange
        var places = new List<Place>
        {
            CreatePlace(Guid.NewGuid(), "Colosseum", PlaceCategory.Historical),
            CreatePlace(Guid.NewGuid(), "InfoCenter", PlaceCategory.Information),
            CreatePlace(Guid.NewGuid(), "Supermarket", PlaceCategory.Supermarket),
            CreatePlace(Guid.NewGuid(), "Zoo", PlaceCategory.Zoo),
        };

        var scored = new List<ScoredPlaceResult>
        {
            new() { Place = places[0], FinalScore = 30, GroupScore = 10, StyleScoreAvg = 10, GoogleMatchBonus = 10 },
            new() { Place = places[1], FinalScore = 0, GroupScore = 0, StyleScoreAvg = 0, GoogleMatchBonus = 0 },
            new() { Place = places[2], FinalScore = -30, GroupScore = -10, StyleScoreAvg = -10, GoogleMatchBonus = -10 },
            new() { Place = places[3], FinalScore = -40, GroupScore = -20, StyleScoreAvg = -10, GoogleMatchBonus = -10 },
        };

        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Standard))
            .ReturnsAsync(places);
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(places, TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(scored);
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Moderate)).Returns(5);

        SetupMapperForPlaces(places, scored);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle> { TravelStyle.Cultural }, Tempo.Moderate, TransportPreference.Walking, new List<Guid>());

        // Assert
        result.Recommended.Should().HaveCount(2);
        result.Neutral.Should().HaveCount(1);
        result.Other.Should().HaveCount(1);
        result.Other[0].Name.Should().Be("Zoo");
    }

    [Fact]
    public async Task GetRecommendedPlaces_ExcludesAlreadyAdded()
    {
        // Arrange
        var excludedId = Guid.NewGuid();
        var places = new List<Place>
        {
            CreatePlace(excludedId, "Colosseum", PlaceCategory.Historical),
            CreatePlace(Guid.NewGuid(), "Vatican", PlaceCategory.Museum),
        };

        var scored = new List<ScoredPlaceResult>
        {
            new() { Place = places[1], FinalScore = 25, GroupScore = 10, StyleScoreAvg = 5, GoogleMatchBonus = 10 },
        };

        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Standard))
            .ReturnsAsync(places);
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(
                It.Is<List<Place>>(list => list.Count == 1 && list[0].Id == places[1].Id),
                TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(scored);
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Moderate)).Returns(5);

        SetupMapperForPlace(places[1], scored[0]);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle> { TravelStyle.Cultural }, Tempo.Moderate, TransportPreference.Walking,
            new List<Guid> { excludedId });

        // Assert
        result.Recommended.Should().HaveCount(1);
        result.Recommended[0].Name.Should().Be("Vatican");
    }

    [Fact]
    public async Task GetRecommendedPlaces_EmptyCity_ReturnsEmpty()
    {
        // Arrange
        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("EmptyCity", BudgetTier.Standard))
            .ReturnsAsync(new List<Place>());
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(new List<Place>(), TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(new List<ScoredPlaceResult>());
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Fast)).Returns(7);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedPlacesAsync(
            "EmptyCity", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle>(), Tempo.Fast, TransportPreference.Walking, new List<Guid>());

        // Assert
        result.Recommended.Should().BeEmpty();
        result.Neutral.Should().BeEmpty();
        result.Other.Should().BeEmpty();
        result.DailyCapacity.Should().Be(7);
    }

    [Fact]
    public async Task GetRecommendedPlaces_LastQuartileFlowsIntoOtherBucketInRankOrder()
    {
        // Arrange
        var places = new List<Place>
        {
            CreatePlace(Guid.NewGuid(), "Top1", PlaceCategory.Historical),
            CreatePlace(Guid.NewGuid(), "Top2", PlaceCategory.Museum),
            CreatePlace(Guid.NewGuid(), "Mid1", PlaceCategory.Cafe),
            CreatePlace(Guid.NewGuid(), "Mid2", PlaceCategory.Park),
            CreatePlace(Guid.NewGuid(), "Low1", PlaceCategory.Mall),
            CreatePlace(Guid.NewGuid(), "Low2", PlaceCategory.Supermarket),
            CreatePlace(Guid.NewGuid(), "Other1", PlaceCategory.Zoo),
            CreatePlace(Guid.NewGuid(), "Other2", PlaceCategory.ThemePark),
        };

        var scored = new List<ScoredPlaceResult>
        {
            new() { Place = places[0], FinalScore = 40, GroupScore = 20, StyleScoreAvg = 10, GoogleMatchBonus = 10 },
            new() { Place = places[1], FinalScore = 35, GroupScore = 20, StyleScoreAvg = 10, GoogleMatchBonus = 5 },
            new() { Place = places[2], FinalScore = 20, GroupScore = 10, StyleScoreAvg = 10, GoogleMatchBonus = 0 },
            new() { Place = places[3], FinalScore = 15, GroupScore = 10, StyleScoreAvg = 5, GoogleMatchBonus = 0 },
            new() { Place = places[4], FinalScore = 5, GroupScore = 5, StyleScoreAvg = 0, GoogleMatchBonus = 0 },
            new() { Place = places[5], FinalScore = 0, GroupScore = 0, StyleScoreAvg = 0, GoogleMatchBonus = 0 },
            new() { Place = places[6], FinalScore = -10, GroupScore = -10, StyleScoreAvg = 0, GoogleMatchBonus = 0 },
            new() { Place = places[7], FinalScore = -20, GroupScore = -10, StyleScoreAvg = -10, GoogleMatchBonus = 0 },
        };

        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Standard))
            .ReturnsAsync(places);
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(places, TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(scored);
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Moderate)).Returns(5);

        SetupMapperForPlaces(places, scored);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle>(), Tempo.Moderate, TransportPreference.Walking, new List<Guid>());

        // Assert
        result.Recommended.Should().HaveCount(4);
        result.Neutral.Should().HaveCount(2);
        result.Other.Should().HaveCount(2);
        result.Other[0].Name.Should().Be("Other1");
        result.Other[1].Name.Should().Be("Other2");
    }

    [Fact]
    public async Task GetRecommendedPlaces_PassesBudgetTierToRepository()
    {
        // Arrange
        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Premium))
            .ReturnsAsync(new List<Place>());
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(new List<Place>(), It.IsAny<TravelCompanion>(), It.IsAny<List<TravelStyle>>()))
            .Returns(new List<ScoredPlaceResult>());
        _timelineMock.Setup(t => t.GetDailyCapacity(It.IsAny<Tempo>())).Returns(3);

        var sut = CreateSut();

        // Act
        await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Premium, TravelCompanion.Couple,
            new List<TravelStyle>(), Tempo.Slow, TransportPreference.PublicTransport, new List<Guid>());

        // Assert
        _placeRepoMock.Verify(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Premium), Times.Once);
    }

    [Fact]
    public async Task GetRecommendedPlaces_WithWalkingPreference_PenalizesFarPlaces()
    {
        var nearPlace = CreatePlace(Guid.NewGuid(), "Near Museum", PlaceCategory.Museum);
        nearPlace.Latitude = 41.005;
        nearPlace.Longitude = 28.978;

        var farPlace = CreatePlace(Guid.NewGuid(), "Far Museum", PlaceCategory.Museum);
        farPlace.Latitude = 41.090;
        farPlace.Longitude = 29.200;

        var places = new List<Place> { farPlace, nearPlace };
        var scored = new List<ScoredPlaceResult>
        {
            new() { Place = farPlace, FinalScore = 20, GroupScore = 10, StyleScoreAvg = 5, GoogleMatchBonus = 5 },
            new() { Place = nearPlace, FinalScore = 20, GroupScore = 10, StyleScoreAvg = 5, GoogleMatchBonus = 5 }
        };

        _placeRepoMock.Setup(r => r.GetByCityAndBudgetTierAsync("Rome", BudgetTier.Standard))
            .ReturnsAsync(places);
        _scoringMock.Setup(s => s.ScoreAndSortPlaces(places, TravelCompanion.Solo, It.IsAny<List<TravelStyle>>()))
            .Returns(scored);
        _timelineMock.Setup(t => t.GetDailyCapacity(Tempo.Moderate)).Returns(5);

        SetupMapperForPlaces(places, scored);

        var sut = CreateSut();

        var result = await sut.GetRecommendedPlacesAsync(
            "Rome", BudgetTier.Standard, TravelCompanion.Solo,
            new List<TravelStyle> { TravelStyle.Cultural }, Tempo.Moderate, TransportPreference.Walking,
            new List<Guid>(), 41.0082, 28.9784);

        result.Recommended.Should().HaveCount(1);
        result.Recommended[0].Name.Should().Be("Near Museum");
        result.Neutral.Should().ContainSingle(r => r.Name == "Far Museum");
    }

    #endregion

    #region Helpers

    private static Place CreatePlace(Guid id, string name, PlaceCategory category)
    {
        return new Place
        {
            Id = id,
            Name = name,
            Category = category,
            City = "Rome",
            Country = "Italy",
            IsActive = true,
            BudgetTiers = new List<BudgetTier> { BudgetTier.Standard },
            GoogleTags = new List<string>(),
            PhotoUrls = new List<string>(),
            TravelStyles = new List<TravelStyle>(),
            BestMonths = new List<int>()
        };
    }

    private void SetupMapperForPlace(Place place, ScoredPlaceResult scored)
    {
        _mapperMock.Setup(m => m.Map<ScoredPlaceResponse>(place))
            .Returns(new ScoredPlaceResponse
            {
                Id = place.Id,
                Name = place.Name,
                Category = place.Category,
                City = place.City,
                Country = place.Country,
                FinalScore = scored.FinalScore,
                GroupScore = scored.GroupScore,
                StyleScoreAvg = scored.StyleScoreAvg,
                GoogleMatchBonus = scored.GoogleMatchBonus
            });
    }

    private void SetupMapperForPlaces(List<Place> places, List<ScoredPlaceResult> scored)
    {
        foreach (var s in scored)
        {
            SetupMapperForPlace(s.Place, s);
        }
    }

    #endregion
}
