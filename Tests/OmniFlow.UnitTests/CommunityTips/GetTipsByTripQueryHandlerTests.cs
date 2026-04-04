using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.CommunityTips.Queries.GetTipsByTrip;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.CommunityTips;

public class GetTipsByTripQueryHandlerTests
{
	private readonly Mock<ICommunityTipRepositoryAsync> _tipRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_TripNotFound_ShouldThrowEntityNotFoundException()
	{
		var tripId = Guid.NewGuid();
		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>()).Object);

		var handler = new GetTipsByTripQueryHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new GetTipsByTripQuery
		{
			TripId = tripId,
			PageNumber = 1,
			PageSize = 10
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ReturnsTipsWithPlaceAndIsUpvotedFlags()
	{
		var currentUserId = Guid.NewGuid();
		var tripId = Guid.NewGuid();
		var placeId = Guid.NewGuid();
		var tipOneId = Guid.NewGuid();
		var tipTwoId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Trips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Trip>
		{
			new() { Id = tripId }
		}).Object);
		_contextMock.Setup(x => x.TipUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<TipUpvote>
		{
			new() { TipId = tipOneId, UserId = currentUserId }
		}).Object);

		var place = new Place
		{
			Id = placeId,
			Name = "Bakery",
			Category = PlaceCategory.Restaurant,
			City = "Antalya",
			Country = "Turkey",
			Latitude = 36.8,
			Longitude = 30.7
		};

		var userOne = new User { Id = Guid.NewGuid(), Username = "alice", Email = "alice@example.com", ProfilePhotoUrl = "https://cdn.example.com/alice.jpg", KarmaScore = 18 };
		var userTwo = new User { Id = Guid.NewGuid(), Username = "bob", Email = "bob@example.com", KarmaScore = 12 };

		var tipOne = new CommunityTip
		{
			Id = tipOneId,
			TripId = tripId,
			PlaceId = placeId,
			Content = "Go early",
			UpvoteCount = 5,
			User = userOne,
			Place = place
		};

		var tipTwo = new CommunityTip
		{
			Id = tipTwoId,
			TripId = tripId,
			Content = "Bring cash",
			UpvoteCount = 2,
			User = userTwo
		};

		_tipRepositoryMock.Setup(x => x.GetByTripAsync(tripId, It.IsAny<OmniFlow.Application.Parameters.RequestParameter>()))
			.ReturnsAsync(new PagedResponse<CommunityTip>(new List<CommunityTip> { tipOne, tipTwo }, 1, 10, 2));

		var handler = new GetTipsByTripQueryHandler(
			_tipRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new GetTipsByTripQuery
		{
			TripId = tripId,
			PageNumber = 1,
			PageSize = 10
		}, CancellationToken.None);

		result.Data.Should().HaveCount(2);
		result.Data[0].Id.Should().Be(tipOneId);
		result.Data[0].IsUpvoted.Should().BeTrue();
		result.Data[0].Place.Should().NotBeNull();
		result.Data[0].Place!.Name.Should().Be("Bakery");
		result.Data[0].Username.Should().Be("alice");
		result.Data[1].Id.Should().Be(tipTwoId);
		result.Data[1].IsUpvoted.Should().BeFalse();
		result.Data[1].Place.Should().BeNull();
	}
}