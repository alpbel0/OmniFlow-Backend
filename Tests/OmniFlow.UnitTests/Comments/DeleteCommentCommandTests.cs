using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Comments.Commands.DeleteComment;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Comments;

public class DeleteCommentCommandHandlerTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();

	[Fact]
	public async Task Handle_CommentNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new DeleteCommentCommand { CommentId = Guid.NewGuid() };
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(command.CommentId)).ReturnsAsync((Comment?)null);

		var handler = new DeleteCommentCommandHandler(_commentRepositoryMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_NotOwner_ShouldThrowForbiddenException()
	{
		var command = new DeleteCommentCommand { CommentId = Guid.NewGuid() };
		var currentUserId = Guid.NewGuid();
		var ownerId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(command.CommentId)).ReturnsAsync(new Comment
		{
			Id = command.CommentId,
			UserId = ownerId,
			Post = new Post { Id = Guid.NewGuid(), CommentCount = 2 }
		});

		var handler = new DeleteCommentCommandHandler(_commentRepositoryMock.Object, _authenticatedUserServiceMock.Object);

		await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidOwner_ShouldSoftDeleteAndDecrementPostCount()
	{
		var currentUserId = Guid.NewGuid();
		var command = new DeleteCommentCommand { CommentId = Guid.NewGuid() };
		var post = new Post { Id = Guid.NewGuid(), CommentCount = 3 };
		var comment = new Comment
		{
			Id = command.CommentId,
			UserId = currentUserId,
			Post = post
		};

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(command.CommentId)).ReturnsAsync(comment);

		var handler = new DeleteCommentCommandHandler(_commentRepositoryMock.Object, _authenticatedUserServiceMock.Object);

		var result = await handler.Handle(command, CancellationToken.None);

		result.Should().Be(MediatR.Unit.Value);
		post.CommentCount.Should().Be(2);
		_commentRepositoryMock.Verify(x => x.DeleteAsync(comment), Times.Once);
	}
}