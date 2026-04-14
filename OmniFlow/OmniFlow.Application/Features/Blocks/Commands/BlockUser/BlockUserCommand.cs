using MediatR;

namespace OmniFlow.Application.Features.Blocks.Commands.BlockUser;

public class BlockUserCommand : IRequest<Unit>
{
	public Guid UserId { get; set; }
}