using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Services;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Notifications;

public class NotificationServiceTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();

	private NotificationService CreateSut(List<Notification> notifications, Action<Notification>? onAdd = null)
	{
		var notificationsSet = MockDbSetHelper.CreateAsyncMockDbSet(notifications);
		notificationsSet
			.Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
			.Returns((Notification entity, CancellationToken _) =>
			{
				onAdd?.Invoke(entity);
				notifications.Add(entity);
				return ValueTask.FromResult<EntityEntry<Notification>>(null!);
			});

		_contextMock.Setup(x => x.Notifications).Returns(notificationsSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		return new NotificationService(_contextMock.Object);
	}

	[Fact]
	public async Task CreateNotificationAsync_SelfNotification_DoesNotCreateNotification()
	{
		var userId = Guid.NewGuid();
		var notifications = new List<Notification>();

		var service = CreateSut(notifications);

		await service.CreateNotificationAsync(
			userId,
			userId,
			NotificationType.PostUpvote,
			Guid.NewGuid(),
			NotificationTargetType.Post);

		notifications.Should().BeEmpty();
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task CreateNotificationAsync_FollowType_SetsTargetNull()
	{
		var userId = Guid.NewGuid();
		var actorId = Guid.NewGuid();
		var notifications = new List<Notification>();
		Notification? createdNotification = null;

		var service = CreateSut(notifications, notification => createdNotification = notification);

		await service.CreateNotificationAsync(
			userId,
			actorId,
			NotificationType.Follow,
			Guid.NewGuid(),
			NotificationTargetType.Post);

		createdNotification.Should().NotBeNull();
		createdNotification!.UserId.Should().Be(userId);
		createdNotification.ActorId.Should().Be(actorId);
		createdNotification.NotificationType.Should().Be(NotificationType.Follow);
		createdNotification.TargetId.Should().BeNull();
		createdNotification.TargetType.Should().BeNull();
		notifications.Should().ContainSingle();
		_contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}
}