using MediatR;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminUsers;

public class GetAdminUsersQuery : IRequest<PagedResponse<AdminUserListItemResponse>>
{
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 20;
	public string? Search { get; set; }
}
