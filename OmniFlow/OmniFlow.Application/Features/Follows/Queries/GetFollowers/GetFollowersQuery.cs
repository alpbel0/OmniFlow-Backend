using MediatR;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Follows.Queries.GetFollowers;

public class GetFollowersQuery : IRequest<PagedResponse<FollowUserResponse>>
{
	public Guid UserId { get; set; }
	public GetFollowersParameter Parameter { get; set; } = new();
}
