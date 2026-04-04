using OmniFlow.Application.DTOs.Posts;

namespace OmniFlow.Application.Features.Posts.Queries.GetFeed;

public class GetFeedViewModel
{
	public IReadOnlyList<PostResponse> Data { get; init; } = new List<PostResponse>();
	public string? NextCursor { get; init; }
	public bool HasMore { get; init; }
}
