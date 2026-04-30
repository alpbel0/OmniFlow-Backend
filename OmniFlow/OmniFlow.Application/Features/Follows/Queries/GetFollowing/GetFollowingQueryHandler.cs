using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Helpers;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Follows.Queries.GetFollowing;

public class GetFollowingQueryHandler : IRequestHandler<GetFollowingQuery, PagedResponse<FollowUserResponse>>
{
	private readonly IFollowRepositoryAsync _followRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly IMapper _mapper;

	public GetFollowingQueryHandler(
		IFollowRepositoryAsync followRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		IMapper mapper)
	{
		_followRepository = followRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_mapper = mapper;
	}

	public async Task<PagedResponse<FollowUserResponse>> Handle(GetFollowingQuery request, CancellationToken cancellationToken)
	{
		var userExists = _context.Users.Any(user => user.Id == request.UserId);
		if (!userExists)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		Guid? currentUserId = null;
		if (Guid.TryParse(_authenticatedUserService.UserId, out var parsedUserId))
		{
			currentUserId = parsedUserId;
		}

		if (currentUserId.HasValue && currentUserId.Value != request.UserId)
		{
			var hasBlockRelationship = await BlockVisibilityHelper.HasBlockRelationshipAsync(
				_context,
				currentUserId.Value,
				request.UserId,
				cancellationToken);

			if (hasBlockRelationship)
			{
				throw new EntityNotFoundException("User", request.UserId);
			}
		}

		var parameter = new RequestParameter
		{
			PageNumber = request.Parameter.PageNumber,
			PageSize = request.Parameter.PageSize
		};

		var pagedFollowing = await _followRepository.GetFollowingAsync(request.UserId, parameter, request.Parameter.SearchTerm);
		var followingUsers = pagedFollowing.Data.Select(follow => follow.Following!).ToList();
		var response = _mapper.Map<List<FollowUserResponse>>(followingUsers);

		if (currentUserId.HasValue && response.Any())
		{
			var userIds = response.Select(user => user.Id).ToList();
			var followedIds = await _context.Follows
				.Where(follow => follow.FollowerId == currentUserId.Value && userIds.Contains(follow.FollowingId))
				.Select(follow => follow.FollowingId)
				.ToListAsync(cancellationToken);

			foreach (var user in response)
			{
				user.IsFollowing = followedIds.Contains(user.Id);
			}
		}

		return new PagedResponse<FollowUserResponse>(response, pagedFollowing.PageNumber, pagedFollowing.PageSize, pagedFollowing.TotalCount);
	}
}