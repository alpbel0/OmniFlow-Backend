using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Features.Posts.Queries.GetFeed;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Mappings;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Posts;

public class GetFeedQueryTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly Mock<IAuthenticatedUserService> _authenticatedUserServiceMock = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<GeneralProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task Handle_LatestTab_ReturnsVisiblePostsOrderedByCreatedAtDescending()
    {
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var newerPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), "newer", isVisible: true);
        var olderPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-2), "older", isVisible: true);
        var hiddenPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-3), "hidden", isVisible: false);
        var deletedPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-4), "deleted", isVisible: true, deletedAt: DateTime.UtcNow.AddMinutes(-1));

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            hiddenPost,
            deletedPost,
            olderPost,
            newerPost
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>()).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.Latest,
            PageSize = 20
        }), CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.Data[0].Id.Should().Be(newerPost.Id);
        result.Data[1].Id.Should().Be(olderPost.Id);
        result.HasMore.Should().BeFalse();
        result.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FollowingTab_ReturnsOnlyFollowedUsersPosts()
    {
        var userId = Guid.NewGuid();
        var followedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var followedPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), "followed", userId: followedUserId);
        var otherPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-2), "other", userId: otherUserId);

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            followedPost,
            otherPost
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>
        {
            new() { FollowerId = userId, FollowingId = followedUserId }
        }).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>()).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.Following,
            PageSize = 20
        }), CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.Single().Id.Should().Be(followedPost.Id);
        result.Data.Single().UserId.Should().Be(followedUserId);
    }

    [Fact]
    public async Task Handle_CursorPagination_ReturnsNextCursorAndHasMore()
    {
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var firstPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), "first");
        var secondPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-2), "second");
        var thirdPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-3), "third");

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            thirdPost,
            secondPost,
            firstPost
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>()).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.Latest,
            PageSize = 2
        }), CancellationToken.None);

        result.Data.Should().HaveCount(2);
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNull();

        var decoded = DecodeCursor(result.NextCursor!);
        decoded.Id.Should().Be(secondPost.Id);
    }

    [Fact]
    public async Task Handle_InvalidCursor_TreatsAsNoCursor()
    {
        var userId = Guid.NewGuid();
        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var post = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), "single");

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            post
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>()).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.Latest,
            Cursor = "not-a-valid-cursor",
            PageSize = 20
        }), CancellationToken.None);

        result.Data.Should().HaveCount(1);
        result.Data.Single().Id.Should().Be(post.Id);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_SetsIsUpvotedFlag()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var post = CreatePost(postId, DateTime.UtcNow.AddMinutes(-1), "upvoted");

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            post
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>()).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>
        {
            new() { PostId = postId, UserId = userId }
        }).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.ForYou,
            PageSize = 20
        }), CancellationToken.None);

        result.Data.Single().IsUpvoted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAuthorIsBlocked_ShouldExcludePostFromFeed()
    {
        var userId = Guid.NewGuid();
        var blockedUserId = Guid.NewGuid();

        _authenticatedUserServiceMock.Setup(x => x.UserId).Returns(userId.ToString());

        var blockedPost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-1), "blocked", blockedUserId);
        var visiblePost = CreatePost(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-2), "visible", Guid.NewGuid());

        _contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
        {
            blockedPost,
            visiblePost
        }).Object);
        _contextMock.Setup(x => x.Follows).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Follow>()).Object);
        _contextMock.Setup(x => x.Blocks).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Block>
        {
            new() { BlockerId = userId, BlockedUserId = blockedUserId }
        }).Object);
        _contextMock.Setup(x => x.PostUpvotes).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<PostUpvote>()).Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new GetFeedQuery(new GetFeedParameter
        {
            Tab = FeedTab.Latest,
            PageSize = 20
        }), CancellationToken.None);

        result.Data.Should().ContainSingle();
        result.Data[0].Content.Should().Be("visible");
    }

    private GetFeedQueryHandler CreateHandler()
    {
        return new GetFeedQueryHandler(_contextMock.Object, _authenticatedUserServiceMock.Object, _mapper);
    }

    private static Post CreatePost(Guid id, DateTime createdAt, string content, Guid? userId = null, bool isVisible = true, DateTime? deletedAt = null)
    {
        var ownerId = userId ?? Guid.NewGuid();
        return new Post
        {
            Id = id,
            UserId = ownerId,
            Content = content,
            CreatedAt = createdAt,
            IsVisible = isVisible,
            DeletedAt = deletedAt,
            User = new User
            {
                Id = ownerId,
                Username = $"user-{ownerId:N}",
                Email = $"user-{ownerId:N}@example.com",
                KarmaScore = 12
            }
        };
    }

    private static FeedCursorInfo DecodeCursor(string cursor)
    {
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        return JsonSerializer.Deserialize<FeedCursorInfo>(json)!;
    }

    private sealed class FeedCursorInfo
    {
        public long CreatedAtTicks { get; set; }
        public Guid Id { get; set; }
    }
}