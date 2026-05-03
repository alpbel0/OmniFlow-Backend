using Microsoft.EntityFrameworkCore;
using Moq;
using OmniFlow.Application.Features.Posts.Queries.GetTrendingTags;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.UnitTests;

namespace OmniFlow.UnitTests.Posts;

public class GetTrendingTagsQueryTests
{
	private readonly Mock<IApplicationDbContext> _contextMock = new();

	[Fact]
	public async Task Handle_ReturnsRecentVisibleTagsSortedByCountAndNormalized()
	{
		var now = DateTime.UtcNow;

		var recentFirst = CreatePost(now.AddDays(-1), ["#İstanbul", "#yemek"]);
		var recentSecond = CreatePost(now.AddDays(-2), ["istanbul", "yemek"]);
		var recentThird = CreatePost(now.AddDays(-3), ["istanbul"]);
		var oldPost = CreatePost(now.AddDays(-8), ["istanbul"]);
		var hiddenPost = CreatePost(now.AddDays(-1), ["ankara"], isVisible: false);
		var deletedPost = CreatePost(now.AddDays(-1), ["izmir"], deletedAt: now.AddDays(-1));

		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			recentFirst,
			recentSecond,
			recentThird,
			oldPost,
			hiddenPost,
			deletedPost
		}).Object);

		var handler = new GetTrendingTagsQueryHandler(_contextMock.Object);

		var result = await handler.Handle(new GetTrendingTagsQuery { Limit = 2, Days = 7 }, CancellationToken.None);

		result.Should().HaveCount(2);
		result[0].Tag.Should().Be("İstanbul");
		result[0].Count.Should().Be(3);
		result[1].Tag.Should().Be("yemek");
		result[1].Count.Should().Be(2);
	}

	[Fact]
	public async Task Handle_WhenNoRecentVisiblePosts_ReturnsEmptyList()
	{
		var now = DateTime.UtcNow;
		var oldPost = CreatePost(now.AddDays(-9), ["tag"]);

		_contextMock.Setup(x => x.Posts).Returns(MockDbSetHelper.CreateAsyncMockDbSet(new List<Post>
		{
			oldPost
		}).Object);

		var handler = new GetTrendingTagsQueryHandler(_contextMock.Object);

		var result = await handler.Handle(new GetTrendingTagsQuery(), CancellationToken.None);

		result.Should().BeEmpty();
	}

	private static Post CreatePost(DateTime createdAt, List<string> tags, bool isVisible = true, DateTime? deletedAt = null)
	{
		return new Post
		{
			Id = Guid.NewGuid(),
			UserId = Guid.NewGuid(),
			CreatedAt = createdAt,
			IsVisible = isVisible,
			DeletedAt = deletedAt,
			Tags = tags
		};
	}
}