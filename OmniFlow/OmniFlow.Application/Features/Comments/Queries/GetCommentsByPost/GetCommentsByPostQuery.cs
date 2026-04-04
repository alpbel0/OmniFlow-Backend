using MediatR;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Comments.Queries.GetCommentsByPost;

public class GetCommentsByPostQuery : IRequest<PagedResponse<CommentResponse>>
{
	public Guid PostId { get; set; }
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}
