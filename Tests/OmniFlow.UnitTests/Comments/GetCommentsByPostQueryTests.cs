using AutoMapper;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Comments.Queries.GetCommentsByPost;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Mappings;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Comments;

public class GetCommentsByPostQueryHandlerStandaloneTests
{
	private readonly Mock<ICommentRepositoryAsync> _commentRepositoryMock = new();
	private readonly Mock<IApplicationDbContext> _contextMock = new();
	private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
	private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

	[Fact]
	public async Task Handle_ExistingComments_ShouldReturnPagedResponse()
	{
		var postId = Guid.NewGuid();
		var userId = Guid.NewGuid();
		var rootCommentId = Guid.NewGuid();

		_authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());
		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			new() { Id = postId }
		}).Object);

		var rootComment = new Comment
		{
			Id = rootCommentId,
			PostId = postId,
			UserId = Guid.NewGuid(),
			Content = "Root",
			CreatedAt = DateTime.UtcNow,
			User = new User { Id = Guid.NewGuid(), Username = "commenter", Email = "commenter@example.com" },
			Replies = new List<Comment>()
		};

		_commentRepositoryMock.Setup(x => x.GetByPostAsync(postId, It.IsAny<RequestParameter>()))
			.ReturnsAsync(new PagedResponse<Comment>(new List<Comment> { rootComment }, 1, 10, 1));

		_contextMock.Setup(x => x.CommentUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<CommentUpvote>
		{
			new() { CommentId = rootCommentId, UserId = userId }
		}).Object);

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

		result.PageNumber.Should().Be(1);
		result.PageSize.Should().Be(10);
		result.TotalCount.Should().Be(1);
		result.Data.Should().ContainSingle();
		result.Data[0].Id.Should().Be(rootCommentId);
		result.Data[0].IsUpvoted.Should().BeTrue();
	}

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
}