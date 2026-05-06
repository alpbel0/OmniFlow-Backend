using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Admin;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminUsers;

public class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PagedResponse<AdminUserListItemResponse>>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetAdminUsersQueryHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<PagedResponse<AdminUserListItemResponse>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
	{
		await EnsureAdminAsync(cancellationToken);

		var search = request.Search?.Trim();
		var query = _context.Users
			.AsNoTracking()
			.Where(user => user.DeletedAt == null);

		if (!string.IsNullOrWhiteSpace(search))
		{
			query = query.Where(user =>
				EF.Functions.Like(user.Username, $"%{search}%") ||
				EF.Functions.Like(user.Email, $"%{search}%"));
		}

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.OrderByDescending(user => user.CreatedAt)
			.Skip((request.PageNumber - 1) * request.PageSize)
			.Take(request.PageSize)
			.Select(user => new AdminUserListItemResponse
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				ProfilePhotoUrl = user.ProfilePhotoUrl,
				Role = user.Role.ToString(),
				IsVerified = user.IsVerified,
				IsSuspended = user.IsSuspended,
				TripCount = _context.Trips.Count(trip => trip.OwnerId == user.Id && trip.DeletedAt == null),
				PostCount = _context.Posts.Count(post => post.UserId == user.Id && post.DeletedAt == null),
				CreatedAt = user.CreatedAt
			})
			.ToListAsync(cancellationToken);

		return new PagedResponse<AdminUserListItemResponse>(
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
