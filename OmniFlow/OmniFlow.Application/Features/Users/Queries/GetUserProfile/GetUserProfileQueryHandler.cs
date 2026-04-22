using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
{
	private readonly IUserRepositoryAsync _userRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetUserProfileQueryHandler(
		IUserRepositoryAsync userRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_userRepository = userRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
	{
		var user = await ResolveUserAsync(request.UserKey);

		if (user == null)
		{
			throw new EntityNotFoundException("User", request.UserKey);
		}

		var response = _mapper.Map<UserProfileResponse>(user);
		response.TripCount = (await _context.Trips
			.Where(trip => trip.OwnerId == user.Id && trip.DeletedAt == null)
			.ToListAsync(cancellationToken)).Count;
		response.PostCount = (await _context.Posts
			.Where(post => post.UserId == user.Id && post.DeletedAt == null)
			.ToListAsync(cancellationToken)).Count;
		response.IsFollowing = await IsFollowingCurrentUserAsync(user.Id, cancellationToken);

		if (Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId) && currentUserId != user.Id)
		{
			var isBlockedByMe = await _context.Blocks.AnyAsync(
				block => block.BlockerId == currentUserId && block.BlockedUserId == user.Id,
				cancellationToken);

			var isBlockedByThem = await _context.Blocks.AnyAsync(
				block => block.BlockerId == user.Id && block.BlockedUserId == currentUserId,
				cancellationToken);

			if (isBlockedByMe || isBlockedByThem)
			{
				response.IsBlocked = true;
				response.IsBlockedByMe = isBlockedByMe;
				response.FollowersCount = 0;
				response.FollowingCount = 0;
				response.TripCount = 0;
				response.PostCount = 0;
				response.IsFollowing = false;
				response.KarmaScore = 0;
			}
		}

		return response;
	}

	private async Task<User?> ResolveUserAsync(string userKey)
	{
		if (Guid.TryParse(userKey, out var userId))
		{
			return await _userRepository.GetByIdAsync(userId);
		}

		return await _userRepository.GetByUsernameAsync(userKey);
	}

	private async Task<bool> IsFollowingCurrentUserAsync(Guid targetUserId, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			return false;
		}

		var followingIds = await _context.Follows
			.Where(follow => follow.FollowerId == currentUserId && follow.FollowingId == targetUserId)
			.Select(follow => follow.FollowingId)
			.ToListAsync(cancellationToken);

		return followingIds.Contains(targetUserId);
	}
}