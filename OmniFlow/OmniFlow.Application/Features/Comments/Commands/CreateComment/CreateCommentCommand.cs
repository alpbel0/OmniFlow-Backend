using MediatR;

namespace OmniFlow.Application.Features.Comments.Commands.CreateComment;

public class CreateCommentCommand : IRequest<Guid>
{
	public Guid PostId { get; set; }
	public Guid? ParentCommentId { get; set; }
	public string Content { get; set; } = string.Empty;
	public List<string> Mentions { get; set; } = new();
}
