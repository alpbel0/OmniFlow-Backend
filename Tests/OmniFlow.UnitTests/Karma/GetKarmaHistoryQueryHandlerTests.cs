using Moq;
using OmniFlow.Application.Features.Karma.Queries.GetKarmaHistory;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Karma;

public class GetKarmaHistoryQueryHandlerTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_ShouldReturnOnlyAuthenticatedUserEvents_WithPaginationAndDescendingCreatedAt()
	{
		var currentUserId = Guid.NewGuid();
		var otherUserId = Guid.NewGuid();

		var olderEvent = new KarmaEvent
		{
			Id = Guid.NewGuid(),
			UserId = currentUserId,
			EventType = KarmaEventType.PostUpvoted,
			Points = 1,
			SourceType = KarmaSourceType.Post,
			CreatedAt = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc)
		};

		var newestEvent = new KarmaEvent
		{
			Id = Guid.NewGuid(),
			UserId = currentUserId,
			ActorId = Guid.NewGuid(),
			EventType = KarmaEventType.TripPublished,
			Points = 10,
			SourceType = KarmaSourceType.Trip,
			CreatedAt = new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc)
		};

		var ignoredEvent = new KarmaEvent
		{
			Id = Guid.NewGuid(),
			UserId = otherUserId,
			EventType = KarmaEventType.TipUpvoted,
			Points = 2,
			SourceType = KarmaSourceType.Tip,
			CreatedAt = new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc)
		};

		var actor = new User
		{
			Id = newestEvent.ActorId!.Value,
			Username = "forker",
			Email = "forker@example.com"
		};

		newestEvent.Actor = actor;

		_contextMock.Setup(x => x.KarmaEvents).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<KarmaEvent>
		{
			olderEvent,
			newestEvent,
			ignoredEvent
		}).Object);

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var handler = new GetKarmaHistoryQueryHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new GetKarmaHistoryQuery
		{
			PageNumber = 1,
			PageSize = 1
		}, CancellationToken.None);

		result.TotalCount.Should().Be(2);
		result.PageNumber.Should().Be(1);
		result.PageSize.Should().Be(1);
		result.Data.Should().HaveCount(1);
		result.Data[0].EventType.Should().Be(KarmaEventType.TripPublished);
		result.Data[0].Points.Should().Be(10);
		result.Data[0].SourceType.Should().Be(KarmaSourceType.Trip);
		result.Data[0].ActorUsername.Should().Be("forker");
	}

	[Fact]
	public async Task Handle_WhenActorMissing_ShouldMapActorUsernameAsNull()
	{
		var currentUserId = Guid.NewGuid();

		_contextMock.Setup(x => x.KarmaEvents).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<KarmaEvent>
		{
			new()
			{
				Id = Guid.NewGuid(),
				UserId = currentUserId,
				ActorId = null,
				EventType = KarmaEventType.PostUpvoted,
				Points = 1,
				SourceType = KarmaSourceType.Post,
				CreatedAt = DateTime.UtcNow
			}
		}).Object);

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());

		var handler = new GetKarmaHistoryQueryHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new GetKarmaHistoryQuery(), CancellationToken.None);

		result.Data.Should().ContainSingle();
		result.Data[0].ActorUsername.Should().BeNull();
	}
}