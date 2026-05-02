using MediatR;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Posts.Queries.GetPostsByUser;

public class GetPostsByUserQuery : IRequest<PagedResponse<PostResponse>>
{
	public Guid UserId { get; set; }
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 20;
}
