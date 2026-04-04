using MediatR;

namespace OmniFlow.Application.Features.Follows.Commands.FollowUser;

public class FollowUserCommand : IRequest<Unit>
{
	public Guid UserId { get; set; }
}
