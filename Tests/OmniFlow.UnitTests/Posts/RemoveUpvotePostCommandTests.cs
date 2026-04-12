using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Commands.RemoveUpvotePost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Posts;

public class RemoveUpvotePostCommandHandlerTests
{
	private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<IKarmaService> _karmaServiceMock = new();

	[Fact]
	public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new RemoveUpvotePostCommand { PostId = Guid.NewGuid() };
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync((Post?)null);

		var handler = new RemoveUpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_UpvoteNotFound_ShouldThrowEntityNotFoundException()
	{
		var userId = Guid.NewGuid();
		var postId = Guid.NewGuid();
		var post = new Post { Id = postId, UserId = Guid.NewGuid(), UpvoteCount = 5 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(post);

		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<PostUpvote>());
		mockSet.Setup(x => x.FindAsync(new object[] { postId, userId }, default)).ReturnsAsync((PostUpvote?)null);
		_contextMock.Setup(x => x.PostUpvotes).Returns(mockSet.Object);

		var handler = new RemoveUpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new RemoveUpvotePostCommand { PostId = postId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidRemoveUpvote_ShouldDecrementCountAndRevokeKarma()
	{
		var userId = Guid.NewGuid();
		var postId = Guid.NewGuid();
		var postOwnerId = Guid.NewGuid();
		var post = new Post { Id = postId, UserId = postOwnerId, UpvoteCount = 10 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(post);

		var existingUpvote = new PostUpvote { PostId = postId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<PostUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { postId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.PostUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		var result = await handler.Handle(new RemoveUpvotePostCommand { PostId = postId }, CancellationToken.None);

		result.Should().Be(Unit.Value);
		post.UpvoteCount.Should().Be(9);
		mockSet.Verify(x => x.Remove(existingUpvote), Times.Once);
		_karmaServiceMock.Verify(x => x.RevokeKarmaAsync(
			postOwnerId,
			userId,
			KarmaEventType.PostUpvoted,
			postId,
			KarmaSourceType.Post), Times.Once);
	}

	[Fact]
	public async Task Handle_UpvoteCountDoesNotGoBelowZero()
	{
		var userId = Guid.NewGuid();
		var postId = Guid.NewGuid();
		var post = new Post { Id = postId, UserId = Guid.NewGuid(), UpvoteCount = 0 };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(post);

		var existingUpvote = new PostUpvote { PostId = postId, UserId = userId };
		var mockSet = MockDbSetHelper.CreateMockDbSet(new List<PostUpvote> { existingUpvote });
		mockSet.Setup(x => x.FindAsync(new object[] { postId, userId }, default)).ReturnsAsync(existingUpvote);
		_contextMock.Setup(x => x.PostUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new RemoveUpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object);

		await handler.Handle(new RemoveUpvotePostCommand { PostId = postId }, CancellationToken.None);

		post.UpvoteCount.Should().Be(0); // Clamped at 0
	}
}