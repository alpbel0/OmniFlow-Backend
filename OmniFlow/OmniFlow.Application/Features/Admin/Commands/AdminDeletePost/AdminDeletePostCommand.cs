using MediatR;

namespace OmniFlow.Application.Features.Admin.Commands.AdminDeletePost;

public class AdminDeletePostCommand : IRequest<Unit>
{
	public Guid PostId { get; set; }
}
