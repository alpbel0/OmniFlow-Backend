using MediatR;

namespace OmniFlow.Application.Features.Posts.Queries.GetFeed;

public class GetFeedQuery : IRequest<GetFeedViewModel>
{
	public GetFeedParameter Parameter { get; set; }

	public GetFeedQuery(GetFeedParameter parameter)
	{
		Parameter = parameter;
	}
}
