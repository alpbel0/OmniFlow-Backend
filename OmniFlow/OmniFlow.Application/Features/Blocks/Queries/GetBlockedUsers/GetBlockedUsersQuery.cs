using MediatR;
using OmniFlow.Application.DTOs.Blocks;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Blocks.Queries.GetBlockedUsers;

public class GetBlockedUsersQuery : IRequest<PagedResponse<BlockedUserResponse>>
{
	public Guid UserId { get; set; }
	public GetBlockedUsersParameter Parameter { get; set; } = new();
}