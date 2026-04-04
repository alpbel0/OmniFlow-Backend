using MediatR;
using Moq;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Commands.UpvotePost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.UnitTests.Posts;

public class UpvotePostCommandHandlerTests
{
	private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<IKarmaService> _karmaServiceMock = new();
	private readonly Mock<INotificationService> _notificationServiceMock = new();

	[Fact]
	public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new UpvotePostCommand { PostId = Guid.NewGuid() };
		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(command.PostId)).ReturnsAsync((Post?)null);

		var handler = new UpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object, _notificationServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_DuplicateUpvote_ShouldThrowDuplicateUpvoteException()
	{
		var userId = Guid.NewGuid();
		var postId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(new Post { Id = postId, UserId = Guid.NewGuid() });

		var existing = new PostUpvote { PostId = postId, UserId = userId };
		var upvotes = new List<PostUpvote> { existing };
		var mockSet = MockDbSetHelper.CreateMockDbSet(upvotes);
		mockSet.Setup(x => x.FindAsync(new object[] { postId, userId }, default)).ReturnsAsync(existing);

		_contextMock.Setup(x => x.PostUpvotes).Returns(mockSet.Object);

		var handler = new UpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object, _notificationServiceMock.Object);

		await Assert.ThrowsAsync<DuplicateUpvoteException>(() => handler.Handle(new UpvotePostCommand { PostId = postId }, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidUpvote_ShouldIncrementCountAndReturnUnit()
	{
		var userId = Guid.NewGuid();
		var postId = Guid.NewGuid();
		var post = new Post { Id = postId, UserId = Guid.NewGuid(), UpvoteCount = 2, IsVisible = true };

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(post);

		var upvotes = new List<PostUpvote>();
		var mockSet = MockDbSetHelper.CreateMockDbSet(upvotes);
		_contextMock.Setup(x => x.PostUpvotes).Returns(mockSet.Object);
		_contextMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

		var handler = new UpvotePostCommandHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _karmaServiceMock.Object, _notificationServiceMock.Object);

		var result = await handler.Handle(new UpvotePostCommand { PostId = postId }, CancellationToken.None);

		result.Should().Be(Unit.Value);
		post.UpvoteCount.Should().Be(3);
		_contextMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
		_karmaServiceMock.Verify(x => x.AwardKarmaAsync(
			post.UserId,
			userId,
			OmniFlow.Domain.Enums.KarmaEventType.PostUpvoted,
			1,
			postId,
			OmniFlow.Domain.Enums.KarmaSourceType.Post), Times.Once);
		_notificationServiceMock.Verify(x => x.CreateNotificationAsync(
			post.UserId,
			userId,
			NotificationType.PostUpvote,
			postId,
			NotificationTargetType.Post), Times.Once);
	}
}
