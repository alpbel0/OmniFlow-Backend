using MediatR;

namespace OmniFlow.Application.Features.Admin.Commands.SetUserSuspended;

public class SetUserSuspendedCommand : IRequest<Unit>
{
	public Guid UserId { get; set; }
	public bool IsSuspended { get; set; }
}
