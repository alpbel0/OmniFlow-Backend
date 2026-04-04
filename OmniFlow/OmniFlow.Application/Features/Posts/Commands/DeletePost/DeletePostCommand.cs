using MediatR;

namespace OmniFlow.Application.Features.Posts.Commands.DeletePost;

public class DeletePostCommand : IRequest<Unit>
{
	public Guid PostId { get; set; }
}
