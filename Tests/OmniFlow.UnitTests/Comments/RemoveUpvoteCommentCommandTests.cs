using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Comments.Commands.RemoveUpvoteComment;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Comments;

public class RemoveUpvoteCommentCommandHandlerTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_CommentNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new RemoveUpvoteCommentCommand { CommentId = Guid.NewGuid() };
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(command.CommentId)).ReturnsAsync((Comment?)null);

		var handler = new RemoveUpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_UpvoteNotFound_ShouldThrowEntityNotFoundException()
	{
		var userId = Guid.NewGuid();
		var commentId = Guid.NewGuid();
		var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), UpvoteCount = 5 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(commentId)).ReturnsAsync(comment);

		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<CommentUpvote>());
		mockSet.Setup(x => x.FindAsync(new object[] { commentId, userId }, default)).ReturnsAsync((CommentUpvote?)null);
		_contextMock.Setup(x => x.CommentUpvotes).Returns(mockSet.Object);

		var handler = new RemoveUpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new RemoveUpvoteCommentCommand { CommentId = commentId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidRemoveUpvote_ShouldDecrementCountAndReturnUnit()
	{
		var userId = Guid.NewGuid();
		var commentId = Guid.NewGuid();
		var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), UpvoteCount = 10 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(commentId)).ReturnsAsync(comment);

		var existingUpvote = new CommentUpvote { CommentId = commentId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<CommentUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { commentId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.CommentUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(new RemoveUpvoteCommentCommand { CommentId = commentId }, CancellationToken.None);

		result.Should().Be(Unit.Value);
		comment.UpvoteCount.Should().Be(9);
		mockSet.Verify(x => x.Remove(existingUpvote), Times.Once);
	}

	[Fact]
	public async Task Handle_UpvoteCountDoesNotGoBelowZero()
	{
		var userId = Guid.NewGuid();
		var commentId = Guid.NewGuid();
		var comment = new Comment { Id = commentId, UserId = Guid.NewGuid(), UpvoteCount = 0 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(commentId)).ReturnsAsync(comment);

		var existingUpvote = new CommentUpvote { CommentId = commentId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<CommentUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { commentId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.CommentUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvoteCommentCommandHandler(_commentRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object);

		await handler.Handle(new RemoveUpvoteCommentCommand { CommentId = commentId }, CancellationToken.None);

		comment.UpvoteCount.Should().Be(0); // Clamped at 0
	}
}