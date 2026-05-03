using MediatR;
using OmniFlow.Application.DTOs.Posts;

namespace OmniFlow.Application.Features.Posts.Queries.GetTrendingTags;

public class GetTrendingTagsQuery : IRequest<IReadOnlyList<TrendingTagResponse>>
{
	public int Limit { get; set; } = 6;

	public int Days { get; set; } = 7;
}