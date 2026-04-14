using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Blocks;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Blocks.Queries.GetBlockedUsers;

public class GetBlockedUsersQueryHandler : IRequestHandler<GetBlockedUsersQuery, PagedResponse<BlockedUserResponse>>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetBlockedUsersQueryHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<PagedResponse<BlockedUserResponse>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
	{
		var userExists = _context.Users.Any(user => user.Id == request.UserId);
		if (!userExists)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		var query = _context.Blocks
			.Include(block => block.BlockedUser)
			.Where(block => block.BlockerId == request.UserId && block.BlockedUser != null)
			.OrderBy(block => block.CreatedAt);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip((request.Parameter.PageNumber - 1) * request.Parameter.PageSize)
			.Take(request.Parameter.PageSize)
			.ToListAsync(cancellationToken);

		var response = items
			.Select(block => new BlockedUserResponse
			{
				Id = block.BlockedUser!.Id,
				Username = block.BlockedUser.Username,
				ProfilePhotoUrl = block.BlockedUser.ProfilePhotoUrl,
				BlockedAt = block.CreatedAt
			})
			.ToList();

		if (Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId) && response.Any())
		{
			var blockedUserIds = response.Select(user => user.Id).ToList();
			var followedIds = await _context.Follows
				.Where(follow => follow.FollowerId == currentUserId && blockedUserIds.Contains(follow.FollowingId))
				.Select(follow => follow.FollowingId)
				.ToListAsync(cancellationToken);

			foreach (var user in response)
			{
				user.IsFollowing = followedIds.Contains(user.Id);
			}
		}

		return new PagedResponse<BlockedUserResponse>(response, request.Parameter.PageNumber, request.Parameter.PageSize, totalCount);
	}
}