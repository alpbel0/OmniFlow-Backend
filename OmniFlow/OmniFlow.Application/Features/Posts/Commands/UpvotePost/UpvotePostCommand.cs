using MediatR;

namespace OmniFlow.Application.Features.Posts.Commands.UpvotePost;

public class UpvotePostCommand : IRequest<Unit>
{
	public Guid PostId { get; set; }
}
