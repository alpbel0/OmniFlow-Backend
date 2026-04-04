using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Notifications.Commands.MarkAllAsRead;
using OmniFlow.Application.Features.Notifications.Commands.MarkAsRead;
using OmniFlow.Application.Features.Notifications.Queries.GetNotifications;
using OmniFlow.Application.Features.Notifications.Queries.GetUnreadCount;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Notifications;

public class GetNotificationsQueryHandlerTests
{
	private readonly Mock<INotificationRepositoryAsync> _notificationRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_ReturnsPagedNotificationsForAuthenticatedUser()
	{
		var userId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

		var response = new PagedResponse<OmniFlow.Application.DTOs.Notifications.NotificationResponse>(
			new List<OmniFlow.Application.DTOs.Notifications.NotificationResponse>
			{
				new()
				{
					Id = Guid.NewGuid(),
					Type = NotificationType.Follow,
					IsRead = false,
					CreatedAt = DateTime.UtcNow,
					ActorUsername = "alice"
				}
			},
			1,
			10,
			1);

		_notificationRepositoryMock
			.Setup(x => x.GetByUserAsync(userId, null, 1, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(response);

		var handler = new GetNotificationsQueryHandler(_notificationRepositoryMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new GetNotificationsQuery
		{
			Parameter = new GetNotificationsParameter
			{
				PageNumber = 1,
				PageSize = 10
			}
		}, CancellationToken.None);

		result.TotalCount.Should().Be(1);
		result.Data.Should().ContainSingle();
		result.Data[0].ActorUsername.Should().Be("alice");
	}
}

public class GetUnreadCountQueryHandlerTests
{
	private readonly Mock<INotificationRepositoryAsync> _notificationRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_ReturnsUnreadCountForAuthenticatedUser()
	{
		var userId = Guid.NewGuid();
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_notificationRepositoryMock.Setup(x => x.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

		var handler = new GetUnreadCountQueryHandler(_notificationRepositoryMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new GetUnreadCountQuery(), CancellationToken.None);

		result.Should().Be(3);
	}
}

public class MarkAsReadCommandHandlerTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_WhenNotificationNotFound_ThrowsEntityNotFoundException()
	{
		var userId = Guid.NewGuid();
		var notificationId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_contextMock.Setup(x => x.Notifications).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Notification>()).Object);

		var handler = new MarkAsReadCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() =>
			handler.Handle(new MarkAsReadCommand { NotificationId = notificationId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WhenNotificationOwnedByAnotherUser_ThrowsForbiddenException()
	{
		var currentUserId = Guid.NewGuid();
		var ownerId = Guid.NewGuid();
		var notificationId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Notifications).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Notification>
		{
			new()
			{
				Id = notificationId,
				UserId = ownerId,
				NotificationType = NotificationType.Follow,
				CreatedAt = DateTime.UtcNow
			}
		}).Object);

		var handler = new MarkAsReadCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<ForbiddenException>(() =>
			handler.Handle(new MarkAsReadCommand { NotificationId = notificationId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WhenOwnedByCurrentUser_MarksAsRead()
	{
		var userId = Guid.NewGuid();
		var notificationId = Guid.NewGuid();
		var notification = new Notification
		{
			Id = notificationId,
			UserId = userId,
			NotificationType = NotificationType.PostUpvote,
			IsRead = false,
			ReadAt = null,
			CreatedAt = DateTime.UtcNow
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_contextMock.Setup(x => x.Notifications).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Notification> { notification }).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var handler = new MarkAsReadCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new MarkAsReadCommand { NotificationId = notificationId }, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		notification.IsRead.Should().BeTrue();
		notification.ReadAt.Should().NotBeNull();
	}
}

public class MarkAllAsReadCommandHandlerTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_MarksOnlyCurrentUsersUnreadNotifications()
	{
		var userId = Guid.NewGuid();
		var otherUserId = Guid.NewGuid();

		var myUnread1 = new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = NotificationType.Follow, IsRead = false };
		var myUnread2 = new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = NotificationType.Comment, IsRead = false };
		var myRead = new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = NotificationType.Fork, IsRead = true, ReadAt = DateTime.UtcNow };
		var othersUnread = new Notification { Id = Guid.NewGuid(), UserId = otherUserId, NotificationType = NotificationType.Mention, IsRead = false };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_contextMock.Setup(x => x.Notifications).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Notification>
		{
			myUnread1,
			myUnread2,
			myRead,
			othersUnread
		}).Object);
		_contextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

		var handler = new MarkAllAsReadCommandHandler(_contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new MarkAllAsReadCommand(), CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		myUnread1.IsRead.Should().BeTrue();
		myUnread2.IsRead.Should().BeTrue();
		myUnread1.ReadAt.Should().NotBeNull();
		myUnread2.ReadAt.Should().NotBeNull();
		othersUnread.IsRead.Should().BeFalse();
	}
}