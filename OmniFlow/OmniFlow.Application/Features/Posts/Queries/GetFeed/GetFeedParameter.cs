using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Posts.Queries.GetFeed;

public class GetFeedParameter
{
	public FeedTab Tab { get; set; } = FeedTab.Latest;
	public string? Cursor { get; set; }
	public int PageSize { get; set; } = 20;
	public string? Query { get; set; }
	public string? Tag { get; set; }
	public PostType? PostType { get; set; }
	public FeedSort Sort { get; set; } = FeedSort.Latest;
}

public enum FeedTab
{
	ForYou,
	Following,
	Latest
}

public enum FeedSort
{
	Latest,
	MostUpvoted,
	MostCommented
}
