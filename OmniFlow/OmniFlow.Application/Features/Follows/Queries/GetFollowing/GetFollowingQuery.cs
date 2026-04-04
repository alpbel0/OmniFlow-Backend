using MediatR;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Follows.Queries.GetFollowing;

public class GetFollowingQuery : IRequest<PagedResponse<FollowUserResponse>>
{
	public Guid UserId { get; set; }
	public GetFollowingParameter Parameter { get; set; } = new();
}
