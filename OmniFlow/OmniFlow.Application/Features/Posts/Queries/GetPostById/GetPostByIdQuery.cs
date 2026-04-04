using MediatR;
using OmniFlow.Application.DTOs.Posts;

namespace OmniFlow.Application.Features.Posts.Queries.GetPostById;

public class GetPostByIdQuery : IRequest<PostResponse>
{
	public Guid PostId { get; set; }
}
