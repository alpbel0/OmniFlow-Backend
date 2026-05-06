using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminPosts;

public class GetAdminPostsQueryHandler : IRequestHandler<GetAdminPostsQuery, PagedResponse<AdminPostListItemResponse>>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetAdminPostsQueryHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<PagedResponse<AdminPostListItemResponse>> Handle(GetAdminPostsQuery request, CancellationToken cancellationToken)
	{
		await EnsureAdminAsync(cancellationToken);

		var search = request.Search?.Trim();
		var query = _context.Posts
			.AsNoTracking()
			.Include(post => post.User)
			.Where(post => post.DeletedAt == null);

		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(post =>
				(post.Content != null && EF.Functions.Like(post.Content, $"%{search}%")) ||
				(post.User != null && EF.Functions.Like(post.User.Username, $"%{search}%")));
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.OrderByDescending(post => post.CreatedAt)
			.Skip((request.PageNumber - 1) * request.PageSize)
			.Take(request.PageSize)
			.Select(post => new AdminPostListItemResponse
			{
				Id = post.Id,
				UserId = post.UserId,
				Username = post.User != null ? post.User.Username : string.Empty,
				ProfilePhotoUrl = post.User != null ? post.User.ProfilePhotoUrl : null,
				Content = post.Content,
				Photos = post.Photos,
				Tags = post.Tags,
				UpvoteCount = post.UpvoteCount,
				CommentCount = post.CommentCount,
				IsVisible = post.IsVisible,
				CreatedAt = post.CreatedAt
			})
			.ToListAsync(cancellationToken);

		return new PagedResponse<AdminPostListItemResponse>(
			items,
			request.PageNumber,
			request.PageSize,
			totalCount);
	}

	private async Task EnsureAdminAsync(CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			throw new ForbiddenException("Admin access required.");
		}

		var role = await _context.Users
			.AsNoTracking()
			.Where(user => user.Id == currentUserId && user.DeletedAt == null)
			.Select(user => user.Role)
			.FirstOrDefaultAsync(cancellationToken);

		if (role != Roles.Admin)
		{
			throw new ForbiddenException("Admin access required.");
		}
	}
}
