using MediatR;

namespace OmniFlow.Application.Features.Blocks.Commands.UnblockUser;

public class UnblockUserCommand : IRequest<Unit>
{
	public Guid UserId { get; set; }
}