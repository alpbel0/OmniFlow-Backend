using MediatR;
using Moq;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Queries.GetRecommendedPlaces;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Phase3;

public class GetRecommendedPlacesHandlerTests
{
    private readonly Mock<ITripRepositoryAsync> _tripRepoMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IRecommendationService> _recommendationServiceMock;
    private readonly Mock<IAuthenticatedUserService> _authServiceMock;
    private readonly ITripVisibilityService _tripVisibilityService = new TripVisibilityService();

    public GetRecommendedPlacesHandlerTests()
    {
        _tripRepoMock = new Mock<ITripRepositoryAsync>();
        _contextMock = new Mock<IApplicationDbContext>();
        _recommendationServiceMock = new Mock<IRecommendationService>();
        _authServiceMock = new Mock<IAuthenticatedUserService>();
    }

    [Fact]
    public async Task ReturnsRecommendedNeutralOther()
    {
        var tripId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var trip = CreateTripWithDestination(tripId, destId, TripStatus.Published);

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var timelineEntries = MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry>());
        _contextMock.Setup(x => x.TimelineEntries).Returns(timelineEntries.Object);

        var expectedResult = new RecommendedPlacesResult
        {
            Recommended = new List<ScoredPlaceResponse> { new ScoredPlaceResponse { Id = Guid.NewGuid(), FinalScore = 30 } },
            Neutral = new List<ScoredPlaceResponse> { new ScoredPlaceResponse { Id = Guid.NewGuid(), FinalScore = 0 } },
            Other = new List<ScoredPlaceResponse> { new ScoredPlaceResponse { Id = Guid.NewGuid(), FinalScore = -10 } },
            DailyCapacity = 5
        };

        _recommendationServiceMock.Setup(x => x.GetRecommendedPlacesAsync(
            "Paris", BudgetTier.Standard, TravelCompanion.Couple,
            It.IsAny<List<TravelStyle>>(), Tempo.Moderate, TransportPreference.Walking,
            It.IsAny<List<Guid>>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        _authServiceMock.Setup(x => x.UserId).Returns(trip.OwnerId.ToString());

        var handler = new GetRecommendedPlacesQueryHandler(
            _tripRepoMock.Object, _contextMock.Object, _recommendationServiceMock.Object, _authServiceMock.Object, _tripVisibilityService);

        var query = new GetRecommendedPlacesQuery { TripId = tripId, DestinationId = destId };
        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Single(result.Recommended);
        Assert.Single(result.Neutral);
        Assert.Single(result.Other);
        Assert.Equal(5, result.DailyCapacity);
    }

    [Fact]
    public async Task ExcludesAlreadyAddedPlaceIds()
    {
        var tripId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var placeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var trip = CreateTripWithDestination(tripId, destId, TripStatus.Draft, ownerId);

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var existingEntry = TimelineEntry.CreatePlaceEntry(tripId, destId, 1, 500.0, placeId);
        var timelineEntries = MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry> { existingEntry });
        _contextMock.Setup(x => x.TimelineEntries).Returns(timelineEntries.Object);

        _recommendationServiceMock.Setup(x => x.GetRecommendedPlacesAsync(
            "Paris", It.IsAny<BudgetTier>(), It.IsAny<TravelCompanion>(),
            It.IsAny<List<TravelStyle>>(), It.IsAny<Tempo>(), It.IsAny<TransportPreference>(),
            It.IsAny<List<Guid>>(), It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendedPlacesResult { DailyCapacity = 5 });

        _authServiceMock.Setup(x => x.UserId).Returns(ownerId.ToString());

        var handler = new GetRecommendedPlacesQueryHandler(
            _tripRepoMock.Object, _contextMock.Object, _recommendationServiceMock.Object, _authServiceMock.Object, _tripVisibilityService);

        var query = new GetRecommendedPlacesQuery { TripId = tripId, DestinationId = destId };
        await handler.Handle(query, CancellationToken.None);

        _recommendationServiceMock.Verify(x => x.GetRecommendedPlacesAsync(
            "Paris", It.IsAny<BudgetTier>(), It.IsAny<TravelCompanion>(),
            It.IsAny<List<TravelStyle>>(), It.IsAny<Tempo>(), It.IsAny<TransportPreference>(),
            It.Is<List<Guid>>(ids => ids.Contains(placeId)),
            It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidTripId_ThrowsEntityNotFound()
    {
        var tripId = Guid.NewGuid();
        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync((Trip?)null);

        var handler = new GetRecommendedPlacesQueryHandler(
            _tripRepoMock.Object, _contextMock.Object, _recommendationServiceMock.Object, _authServiceMock.Object, _tripVisibilityService);

        var query = new GetRecommendedPlacesQuery { TripId = tripId, DestinationId = Guid.NewGuid() };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task DestinationNotBelongToTrip_ThrowsApiException()
    {
        var tripId = Guid.NewGuid();
        var wrongDestId = Guid.NewGuid();
        var actualDestId = Guid.NewGuid();
        var trip = CreateTripWithDestination(tripId, actualDestId, TripStatus.Published);

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var timelineEntries = MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry>());
        _contextMock.Setup(x => x.TimelineEntries).Returns(timelineEntries.Object);

        _authServiceMock.Setup(x => x.UserId).Returns(trip.OwnerId.ToString());

        var handler = new GetRecommendedPlacesQueryHandler(
            _tripRepoMock.Object, _contextMock.Object, _recommendationServiceMock.Object, _authServiceMock.Object, _tripVisibilityService);

        var query = new GetRecommendedPlacesQuery { TripId = tripId, DestinationId = wrongDestId };

        var ex = await Assert.ThrowsAsync<ApiException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Contains("Destination not found", ex.Message);
    }

    [Fact]
    public async Task UsesAccommodationCoordinatesAsHub()
    {
        var tripId = Guid.NewGuid();
        var destId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var trip = CreateTripWithDestination(tripId, destId, TripStatus.Draft, ownerId);

        _tripRepoMock.Setup(x => x.GetByIdWithOwnerAndDestinationsAsync(tripId))
            .ReturnsAsync(trip);

        var accommodationEntry = TimelineEntry.CreateCustomAccommodationEntry(
            tripId,
            destId,
            1,
            500,
            new DateTime(2026, 8, 10, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc),
            "Hotel",
            "Address",
            48.8566,
            2.3522);

        var timelineEntries = MockDbSetHelper.CreateAsyncMockDbSet(new List<TimelineEntry> { accommodationEntry });
        _contextMock.Setup(x => x.TimelineEntries).Returns(timelineEntries.Object);
        _authServiceMock.Setup(x => x.UserId).Returns(ownerId.ToString());

        _recommendationServiceMock.Setup(x => x.GetRecommendedPlacesAsync(
            "Paris",
            BudgetTier.Standard,
            TravelCompanion.Couple,
            It.IsAny<List<TravelStyle>>(),
            Tempo.Moderate,
            TransportPreference.Walking,
            It.IsAny<List<Guid>>(),
            48.8566,
            2.3522,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecommendedPlacesResult { DailyCapacity = 5 });

        var handler = new GetRecommendedPlacesQueryHandler(
            _tripRepoMock.Object, _contextMock.Object, _recommendationServiceMock.Object, _authServiceMock.Object, _tripVisibilityService);

        await handler.Handle(new GetRecommendedPlacesQuery { TripId = tripId, DestinationId = destId }, CancellationToken.None);

        _recommendationServiceMock.VerifyAll();
    }

    private static Trip CreateTripWithDestination(Guid tripId, Guid destId, TripStatus status, Guid? ownerId = null)
    {
        var owner = ownerId ?? Guid.NewGuid();
        return new Trip
        {
            Id = tripId,
            Origin = "Istanbul",
            OriginCountry = "TR",
            Status = status,
            OwnerId = owner,
            BudgetTier = BudgetTier.Standard,
            AdjustedBudgetTier = null,
            TravelCompanion = TravelCompanion.Couple,
            TravelStyles = new List<TravelStyle> { TravelStyle.Cultural, TravelStyle.Relax },
            Tempo = Tempo.Moderate,
            TransportPreference = TransportPreference.Walking,
            Destinations = new List<TripDestination>
            {
                new TripDestination(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13), "Paris", "FR", 1)
                {
                    Id = destId,
                    TripId = tripId
                }
            }
        };
    }
}
