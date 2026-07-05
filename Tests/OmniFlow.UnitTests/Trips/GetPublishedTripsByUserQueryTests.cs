using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Trips.Queries.GetPublishedTripsByUser;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Trips;

public class GetPublishedTripsByUserQueryTests
{
	private readonly Mock<ITripRepositoryAsync> _tripRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(
		cfg => cfg.AddProfile<GeneralProfile>(),
		NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_TargetUserNotFound_ShouldThrowEntityNotFoundException()
	{
		var targetUserId = Guid.NewGuid();
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>()).Object);

		var handler = CreateHandler();

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(
			new GetPublishedTripsByUserQuery { UserId = targetUserId },
			CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WhenBlockedRelationshipExists_ShouldReturnEmptyPage()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = targetUserId, Username = "target", Email = "target@example.com" }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>
		{
			new() { BlockerId = currentUserId, BlockedUserId = targetUserId }
		}).Object);

		var handler = CreateHandler();

		var result = await handler.Handle(
			new GetPublishedTripsByUserQuery { UserId = targetUserId, PageNumber = 2, PageSize = 5 },
			CancellationToken.None);

		result.Data.Should().BeEmpty();
		result.TotalCount.Should().Be(0);
		result.PageNumber.Should().Be(2);
		result.PageSize.Should().Be(5);
		_tripRepositoryMock.Verify(
			x => x.GetPublishedByOwnerAsync(It.IsAny<Guid>(), It.IsAny<RequestParameter>()),
			Times.Never);
	}

	[Fact]
	public async Task Handle_ShouldReturnPublishedTripsPage()
	{
		var currentUserId = Guid.NewGuid();
		var targetUserId = Guid.NewGuid();
		var tripId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = targetUserId, Username = "target", Email = "target@example.com" }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
		_contextMock.Setup(x => x.SavedTrips).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<SavedTrip>()).Object);

		_tripRepositoryMock
			.Setup(x => x.GetPublishedByOwnerAsync(
				targetUserId,
				It.Is<RequestParameter>(p => p.PageNumber == 1 && p.PageSize == 20)))
			.ReturnsAsync(new PagedResponse<Trip>(
				new List<Trip>
				{
					new()
					{
						Id = tripId,
						OwnerId = targetUserId,
						Title = "Published Trip",
						Status = Domain.Enums.TripStatus.Published,
						Owner = new User { Id = targetUserId, Username = "target", Email = "target@example.com" }
					}
				},
				1,
				20,
				1));

		var handler = CreateHandler();

		var result = await handler.Handle(
			new GetPublishedTripsByUserQuery
			{
				UserId = targetUserId,
				PageNumber = 1,
				PageSize = 20
			},
			CancellationToken.None);

		result.Data.Should().HaveCount(1);
		result.TotalCount.Should().Be(1);
		result.Data[0].Id.Should().Be(tripId);
		result.Data[0].Title.Should().Be("Published Trip");
	}

	private GetPublishedTripsByUserQueryHandler CreateHandler()
	{
		return new GetPublishedTripsByUserQueryHandler(
			_tripRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);
	}
}
