using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Comments.Commands.CreateComment;
using OmniFlow.Application.Features.Comments.Queries.GetCommentsByPost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Comments;

public class CreateCommentCommandValidatorTests
{
	private readonly CreateCommentCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidContent_ShouldPass()
	{
		var command = new CreateCommentCommand
		{
			PostId = Guid.NewGuid(),
			Content = "Nice trip"
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyContent_ShouldFail()
	{
		var command = new CreateCommentCommand
		{
			PostId = Guid.NewGuid(),
			Content = "   "
		};

		var result = _validator.Validate(command);

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateCommentCommand.Content));
	}
}

public class CreateCommentCommandHandlerTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly Mock<INotificationService> _notificationServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
	{
		var command = new CreateCommentCommand
		{
			PostId = Guid.NewGuid(),
			Content = "Nice place"
		};

		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>()).Object);

		var handler = new CreateCommentCommandHandler(
			_commentRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_notificationServiceMock.Object);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ParentCommentFromDifferentPost_ShouldThrowEntityNotFoundException()
	{
		var postId = Guid.NewGuid();
		var parentCommentId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = postId, CommentCount = 0 }
		}).Object);
		_commentRepositoryMock.Setup(x => x.GetByIdWithRepliesAsync(parentCommentId)).ReturnsAsync(new Comment
		{
			Id = parentCommentId,
			PostId = Guid.NewGuid(),
			UserId = Guid.NewGuid()
		});

		var handler = new CreateCommentCommandHandler(
			_commentRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_notificationServiceMock.Object);

		var command = new CreateCommentCommand
		{
			PostId = postId,
			ParentCommentId = parentCommentId,
			Content = "Reply"
		};

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(command, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ValidComment_ShouldCreateCommentAndIncrementPostCount()
	{
		var postId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();
		var postOwnerId = Guid.NewGuid();
		var mentionedUserId = Guid.NewGuid();
		var post = new Post { Id = postId, UserId = postOwnerId, CommentCount = 0 };
		Comment? createdComment = null;

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post> { post }).Object);
		_contextMock.Setup(x => x.Users).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<User>
		{
			new() { Id = mentionedUserId, Username = "alice", Email = "alice@example.com" }
		}).Object);
		_commentRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Comment>()))
			.ReturnsAsync((Comment comment) =>
			{
				createdComment = comment;
				return comment;
			});

		var handler = new CreateCommentCommandHandler(
			_commentRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper,
			_notificationServiceMock.Object);

		var command = new CreateCommentCommand
		{
			PostId = postId,
			Content = "Looks great",
			Mentions = new List<string> { "@alice" }
		};

		var result = await handler.Handle(command, CancellationToken.None);

		result.Should().Be(createdComment!.Id);
		createdComment.UserId.Should().Be(currentUserId);
		post.CommentCount.Should().Be(1);
		_commentRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Comment>()), Times.Once);
		_notificationServiceMock.Verify(x => x.CreateNotificationAsync(
			postOwnerId,
			currentUserId,
			NotificationType.Comment,
			postId,
			NotificationTargetType.Post), Times.Once);
		_notificationServiceMock.Verify(x => x.CreateNotificationAsync(
			mentionedUserId,
			currentUserId,
			NotificationType.Mention,
			postId,
			NotificationTargetType.Post), Times.Once);
	}
}

public class GetCommentsByPostQueryHandlerTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
	{
		var postId = Guid.NewGuid();
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>()).Object);

		var handler = new GetCommentsByPostQueryHandler(
			_commentRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new GetCommentsByPostQuery
		{
			PostId = postId,
			PageNumber = 1,
			PageSize = 10
		}, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ExistingComments_ShouldMapRepliesAndSetUpvoteFlags()
	{
		var postId = Guid.NewGuid();
		var currentUserId = Guid.NewGuid();
		var postOwnerId = Guid.NewGuid();
		var rootCommentId = Guid.NewGuid();
		var replyCommentId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(currentUserId.ToString());
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = postId, UserId = postOwnerId }
		}).Object);
		_contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);

		var replyComment = new Comment
		{
			Id = replyCommentId,
			PostId = postId,
			UserId = Guid.NewGuid(),
			Content = "Reply",
			CreatedAt = DateTime.UtcNow.AddMinutes(1),
			User = new User { Id = Guid.NewGuid(), Username = "reply-user", Email = "reply@example.com" }
		};

		var rootComment = new Comment
		{
			Id = rootCommentId,
			PostId = postId,
			UserId = Guid.NewGuid(),
			Content = "Root comment",
			CreatedAt = DateTime.UtcNow,
			User = new User { Id = Guid.NewGuid(), Username = "root-user", Email = "root@example.com" },
			Replies = new List<Comment> { replyComment }
		};

		var comments = new List<Comment> { rootComment };
		_commentRepositoryMock.Setup(x => x.GetByPostAsync(
			postId,
			It.IsAny<OmniFlow.Application.Parameters.RequestParameter>(),
			It.IsAny<IReadOnlyCollection<Guid>>(),
			It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<Comment>(comments, 1, 10, 1));

		var upvotes = new List<CommentUpvote>
		{
			new() { CommentId = rootCommentId, UserId = currentUserId }
		};
		_contextMock.Setup(x => x.CommentUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(upvotes).Object);

		var handler = new GetCommentsByPostQueryHandler(
			_commentRepositoryMock.Object,
			_contextMock.Object,
			_authenticatedUserServiceMock.Object,
			_mapper);

		var result = await handler.Handle(new GetCommentsByPostQuery
		{
			PostId = postId,
			PageNumber = 1,
			PageSize = 10
		}, CancellationToken.None);

		result.Data.Should().HaveCount(1);
		result.Data[0].Id.Should().Be(rootCommentId);
		result.Data[0].IsUpvoted.Should().BeTrue();
		result.Data[0].Replies.Should().HaveCount(1);
		result.Data[0].Replies[0].Id.Should().Be(replyCommentId);
		result.Data[0].Replies[0].IsUpvoted.Should().BeFalse();
		result.Data[0].Replies[0].Username.Should().Be("reply-user");
	}
}
