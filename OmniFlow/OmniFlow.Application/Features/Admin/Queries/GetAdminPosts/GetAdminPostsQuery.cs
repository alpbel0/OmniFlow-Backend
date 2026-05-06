using MediatR;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminPosts;

public class GetAdminPostsQuery : IRequest<PagedResponse<AdminPostListItemResponse>>
{
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public string? Search { get; set; }
}
