using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Trips.Queries.ExploreTrips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class ExploreTripQueryTests
{
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ExploreTripsQueryHandler _handler;

    public ExploreTripQueryTests()
    {
        _contextMock = new Mock<IApplicationDbContext>();
        _authenticatedUserServiceMock = new Mock<IAuthenticatedUserService>();
        _mapperMock = new Mock<IMapper>();
        _handler = new ExploreTripsQueryHandler(
            _contextMock.Object,
            _authenticatedUserServiceMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsPublishedTrips()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trips = new List<Trip>
        {
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, PopularityScore = 100, Owner = new User { Id = Guid.NewGuid(), Username = "user1" } },
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, PopularityScore = 90, Owner = new User { Id = Guid.NewGuid(), Username = "user2" } }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(trips);
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = trips.Select(t => new TripResponse { Id = t.Id }).ToList();
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var upvotes = new List<TripUpvote>().AsQueryable();
        var mockUpvotesSet = CreateMockDbSet(upvotes);
        _contextMock.Setup(x => x.TripUpvotes).Returns(mockUpvotesSet.Object);

        var savedTrips = new List<SavedTrip>().AsQueryable();
        var mockSavedSet = CreateMockDbSet(savedTrips);
        _contextMock.Setup(x => x.SavedTrips).Returns(mockSavedSet.Object);

        var parameter = new ExploreTripsParameter { PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_TravelStyleFilter_ReturnsMatchingTrips()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trips = new List<Trip>
        {
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }, PopularityScore = 100, Owner = new User { Id = Guid.NewGuid(), Username = "user1" } },
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, TravelStyles = new List<TravelStyle> { TravelStyle.Relax }, PopularityScore = 90, Owner = new User { Id = Guid.NewGuid(), Username = "user2" } }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(trips);
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = new List<TripResponse> { new() { Id = trips.First().Id } };
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var upvotes = new List<TripUpvote>().AsQueryable();
        _contextMock.Setup(x => x.TripUpvotes).Returns(CreateMockDbSet(upvotes).Object);

        var savedTrips = new List<SavedTrip>().AsQueryable();
        _contextMock.Setup(x => x.SavedTrips).Returns(CreateMockDbSet(savedTrips).Object);

        var parameter = new ExploreTripsParameter { TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }, PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_CursorPagination_ReturnsNextCursor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trips = new List<Trip>();
        for (int i = 0; i < 15; i++)
        {
            trips.Add(new Trip
            {
                Id = Guid.NewGuid(),
                Status = TripStatus.Published,
                PopularityScore = 100 - i,
                Owner = new User { Id = Guid.NewGuid(), Username = $"user{i}" }
            });
        }

        var mockSet = CreateMockDbSet(trips.AsQueryable());
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = trips.Take(10).Select(t => new TripResponse { Id = t.Id, PopularityScore = t.PopularityScore }).ToList();
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var upvotes = new List<TripUpvote>().AsQueryable();
        _contextMock.Setup(x => x.TripUpvotes).Returns(CreateMockDbSet(upvotes).Object);

        var savedTrips = new List<SavedTrip>().AsQueryable();
        _contextMock.Setup(x => x.SavedTrips).Returns(CreateMockDbSet(savedTrips).Object);

        var parameter = new ExploreTripsParameter { PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_SetsIsUpvotedIsSaved()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tripId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trips = new List<Trip>
        {
            new() { Id = tripId, Status = TripStatus.Published, PopularityScore = 100, Owner = new User { Id = Guid.NewGuid(), Username = "user1" } }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(trips);
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = new List<TripResponse> { new() { Id = tripId } };
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var upvotes = new List<TripUpvote> { new() { TripId = tripId, UserId = userId } }.AsQueryable();
        _contextMock.Setup(x => x.TripUpvotes).Returns(CreateMockDbSet(upvotes).Object);

        var savedTrips = new List<SavedTrip> { new() { TripId = tripId, UserId = userId } }.AsQueryable();
        _contextMock.Setup(x => x.SavedTrips).Returns(CreateMockDbSet(savedTrips).Object);

        var parameter = new ExploreTripsParameter { PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.First().IsUpvoted.Should().BeTrue();
        result.Data.First().IsSaved.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_SetsFlagsNull()
    {
        // Arrange
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(string.Empty);

        var trips = new List<Trip>
        {
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, PopularityScore = 100, Owner = new User { Id = Guid.NewGuid(), Username = "user1" } }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(trips);
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = new List<TripResponse> { new() { Id = Guid.NewGuid() } };
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var parameter = new ExploreTripsParameter { PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Data.First().IsUpvoted.Should().BeNull();
        result.Data.First().IsSaved.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InvalidCursor_TreatsAsNoCursor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var trips = new List<Trip>
        {
            new() { Id = Guid.NewGuid(), Status = TripStatus.Published, PopularityScore = 100, Owner = new User { Id = Guid.NewGuid(), Username = "user1" } }
        }.AsQueryable();

        var mockSet = CreateMockDbSet(trips);
        _contextMock.Setup(x => x.Trips).Returns(mockSet.Object);

        var tripResponses = new List<TripResponse> { new() { Id = Guid.NewGuid() } };
        _mapperMock.Setup(x => x.Map<List<TripResponse>>(It.IsAny<List<Trip>>()))
            .Returns(tripResponses);

        var upvotes = new List<TripUpvote>().AsQueryable();
        _contextMock.Setup(x => x.TripUpvotes).Returns(CreateMockDbSet(upvotes).Object);

        var savedTrips = new List<SavedTrip>().AsQueryable();
        _contextMock.Setup(x => x.SavedTrips).Returns(CreateMockDbSet(savedTrips).Object);

        var parameter = new ExploreTripsParameter { Cursor = "invalid-cursor", PageSize = 10 };
        var query = new ExploreTripsQuery(parameter);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
    }

    private static Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));
        return mockSet;
    }
}