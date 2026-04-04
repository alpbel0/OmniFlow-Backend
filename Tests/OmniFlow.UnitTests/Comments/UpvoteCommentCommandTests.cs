using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Comments.Commands.UpvoteComment;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Comments;

public class UpvoteCommentCommandHandlerTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<INotificationService> _notificationServiceMock = new();

	[Fact]
	public async Task Handle_CommentNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new UpvoteCommentCommand { CommentId = Guid.NewGuid() };
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(command.CommentId)).ReturnsAsync((Comment?)null);

		var handler = new UpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _notificationServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_DuplicateUpvote_ShouldThrowDuplicateUpvoteException()
	{
		var userId = Guid.NewGuid();
		var commentId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(commentId)).ReturnsAsync(new Comment { Id = commentId, UserId = Guid.NewGuid() });

		var existing = new CommentUpvote { CommentId = commentId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<CommentUpvote> { existing });
		mockSet.Setup(x => x.FindAsync(new object[] { commentId, userId }, default)).ReturnsAsync(existing);
		_contextMock.Setup(x => x.CommentUpvotes).Returns(mockSet.Object);

		var handler = new UpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _notificationServiceMock.Object);

		await Assert.ThrowsAsync<DuplicateUpvoteException>(() => handler.Handle(new UpvoteCommentCommand { CommentId = commentId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidUpvote_ShouldIncrementCountAndSave()
	{
		var userId = Guid.NewGuid();
		var commentId = Guid.NewGuid();
		var commentOwnerId = Guid.NewGuid();
		var comment = new Comment { Id = commentId, UserId = commentOwnerId, UpvoteCount = 2 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(commentId)).ReturnsAsync(comment);

		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<CommentUpvote>());
		_contextMock.Setup(x => x.CommentUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new UpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _notificationServiceMock.Object);

		var result = await handler.Handle(new UpvoteCommentCommand { CommentId = commentId }, CancellationToken.None);

		result.Should().Be(Unit.Value);
		comment.UpvoteCount.Should().Be(3);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
		_notificationServiceMock.Verify(x => x.CreateNotificationAsync(
			commentOwnerId,
			userId,
			NotificationType.CommentUpvote,
			commentId,
			NotificationTargetType.Comment), Times.Once);
	}
}