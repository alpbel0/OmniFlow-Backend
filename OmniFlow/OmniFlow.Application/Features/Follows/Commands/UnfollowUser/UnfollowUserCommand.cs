using MediatR;

namespace OmniFlow.Application.Features.Follows.Commands.UnfollowUser;

public class UnfollowUserCommand : IRequest<Unit>
{
	public Guid UserId { get; set; }
}
