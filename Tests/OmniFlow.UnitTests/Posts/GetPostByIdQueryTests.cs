using AutoMapper;
using Moq;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Features.Posts.Queries.GetPostById;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Posts;

public class GetPostByIdQueryHandlerTests
{
    private readonly Mock<IPostRepositoryAsync> _postRepositoryMock = new();
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    [Fact]
    public async Task Handle_PostNotFound_ShouldThrowEntityNotFoundException()
    {
        var postId = Guid.NewGuid();
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync((Post?)null);

        var handler = new GetPostByIdQueryHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _mapperMock.Object);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => handler.Handle(new GetPostByIdQuery { PostId = postId }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingPost_ShouldMapAndSetIsUpvoted()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var post = new Post { Id = postId, UserId = Guid.NewGuid(), IsVisible = true };
        _postRepositoryMock.Setup(x => x.GetByIdWithUserAsync(postId)).ReturnsAsync(post);

        var upvotes = new List<PostUpvote>
        {
            new() { PostId = postId, UserId = userId }
        };
        var upvoteSet = MockDbSetHelper.CreateAsyncMockDbSet(upvotes);
        _contextMock.Setup(x => x.PostUpvotes).Returns(upvoteSet.Object);

        _mapperMock
            .Setup(x => x.Map<PostResponse>(post))
            .Returns(new PostResponse { Id = postId });

        var handler = new GetPostByIdQueryHandler(_postRepositoryMock.Object, _contextMock.Object, _authenticatedUserServiceMock.Object, _mapperMock.Object);

        var result = await handler.Handle(new GetPostByIdQuery { PostId = postId }, CancellationToken.None);

        result.Id.Should().Be(postId);
        result.IsUpvoted.Should().BeTrue();
    }
}
