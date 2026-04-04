namespace OmniFlow.Application.Features.Posts.Queries.GetFeed;

public class GetFeedParameter
{
	public FeedTab Tab { get; set; } = FeedTab.Latest;
	public string? Cursor { get; set; }
	public int PageSize { get; set; } = 20;
}

public enum FeedTab
{
	ForYou,
	Following,
	Latest
}
