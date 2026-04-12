using MediatR;

namespace OmniFlow.Application.Features.Comments.Commands.RemoveUpvoteComment;

/// <summary>
/// Command to remove an upvote from a comment.
/// </summary>
public class RemoveUpvoteCommentCommand : IRequest<Unit>
{
	public Guid CommentId { get; set; }
}