using MediatR;

namespace OmniFlow.Application.Features.Comments.Commands.DeleteComment;

public class DeleteCommentCommand : IRequest<Unit>
{
	public Guid CommentId { get; set; }
}
