using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Karma;

public class KarmaServiceTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();

	private KarmaService CreateSut(List<User> users, List<KarmaEvent> karmaEvents, Action<KarmaEvent>? onAdd = null)
	{
		var usersSet = MockDbSetHelper.CreateAsyncMockDbSet(users);
		var karmaEventsSet = MockDbSetHelper.CreateAsyncMockDbSet(karmaEvents);
		karmaEventsSet
			.Setup(x => x.AddAsync(It.IsAny<KarmaEvent>(), It.IsAny<CancellationToken>()))
			.Returns((KarmaEvent entity, CancellationToken _) =>
			{
				onAdd?.Invoke(entity);
				karmaEvents.Add(entity);
				return ValueTask.FromResult<EntityEntry<KarmaEvent>>(null!);
			});

		_contextMock.Setup(x => x.Users).Returns(usersSet.Object);
		_contextMock.Setup(x => x.KarmaEvents).Returns(karmaEventsSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		return new KarmaService(_contextMock.Object);
	}

	[Fact]
	public async Task AwardKarmaAsync_ValidAward_CreatesEventAndIncreasesKarmaScore()
	{
		var userId = Guid.NewGuid();
		var actorId = Guid.NewGuid();
		var sourceId = Guid.NewGuid();
		var user = new User { Id = userId, KarmaScore = 10 };
		var karmaEvents = new List<KarmaEvent>();
		KarmaEvent? createdEvent = null;

		var service = CreateSut(new List<User> { user }, karmaEvents, entity => createdEvent = entity);

		await service.AwardKarmaAsync(
			userId,
			actorId,
			KarmaEventType.TripPublished,
			10,
			sourceId,
			KarmaSourceType.Trip);

		createdEvent.Should().NotBeNull();
		createdEvent!.UserId.Should().Be(userId);
		createdEvent.ActorId.Should().Be(actorId);
		createdEvent.EventType.Should().Be(KarmaEventType.TripPublished);
		createdEvent.Points.Should().Be(10);
		createdEvent.SourceId.Should().Be(sourceId);
		createdEvent.SourceType.Should().Be(KarmaSourceType.Trip);
		karmaEvents.Should().ContainSingle();
		user.KarmaScore.Should().Be(20);
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task AwardKarmaAsync_DuplicateAward_SkipsWithoutChangingScore()
	{
		var userId = Guid.NewGuid();
		var actorId = Guid.NewGuid();
		var sourceId = Guid.NewGuid();
		var user = new User { Id = userId, KarmaScore = 15 };
		var karmaEvents = new List<KarmaEvent>
		{
			new()
			{
				UserId = userId,
				ActorId = actorId,
				EventType = KarmaEventType.TripPublished,
				Points = 10,
				SourceId = sourceId,
				SourceType = KarmaSourceType.Trip
			}
		};

		var service = CreateSut(new List<User> { user }, karmaEvents);

		await service.AwardKarmaAsync(
			userId,
			actorId,
			KarmaEventType.TripPublished,
			10,
			sourceId,
			KarmaSourceType.Trip);

		karmaEvents.Should().HaveCount(1);
		user.KarmaScore.Should().Be(15);
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task AwardKarmaAsync_UserNotFound_ThrowsEntityNotFoundException()
	{
		var service = CreateSut(new List<User>(), new List<KarmaEvent>());

		var act = () => service.AwardKarmaAsync(
			Guid.NewGuid(),
			Guid.NewGuid(),
			KarmaEventType.TripPublished,
			10,
			Guid.NewGuid(),
			KarmaSourceType.Trip);

		await act.Should().ThrowAsync<EntityNotFoundException>();
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}
}
