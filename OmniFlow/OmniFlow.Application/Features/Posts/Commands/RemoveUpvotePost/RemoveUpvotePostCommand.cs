using MediatR;

namespace OmniFlow.Application.Features.Posts.Commands.RemoveUpvotePost;

/// <summary>
/// Command to remove an upvote from a post.
/// </summary>
public class RemoveUpvotePostCommand : IRequest<Unit>
{
	public Guid PostId { get; set; }
}