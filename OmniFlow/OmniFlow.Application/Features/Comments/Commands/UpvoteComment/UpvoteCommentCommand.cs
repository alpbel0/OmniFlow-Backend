using MediatR;

namespace OmniFlow.Application.Features.Comments.Commands.UpvoteComment;

public class UpvoteCommentCommand : IRequest<Unit>
{
	public Guid CommentId { get; set; }
}
