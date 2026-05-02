using MediatR;
using OmniFlow.Application.DTOs.Users;

namespace OmniFlow.Application.Features.Users.Queries.GetSuggestedFollows;

public class GetSuggestedFollowsQuery : IRequest<IReadOnlyList<SuggestedFollowResponse>>
{
	public int Limit { get; set; } = 6;
	public IEnumerable<Guid> ExcludeUserIds { get; set; } = Array.Empty<Guid>();
}
