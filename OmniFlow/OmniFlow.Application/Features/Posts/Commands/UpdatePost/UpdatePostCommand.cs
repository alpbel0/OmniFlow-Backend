using MediatR;

namespace OmniFlow.Application.Features.Posts.Commands.UpdatePost;

public class UpdatePostCommand : IRequest<Unit>
{
	public Guid PostId { get; set; }
	public string? Content { get; set; }
	public List<string>? Tags { get; set; }
}
