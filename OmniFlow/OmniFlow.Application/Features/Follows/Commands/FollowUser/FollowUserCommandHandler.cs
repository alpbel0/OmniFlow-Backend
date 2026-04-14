using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Follows.Commands.FollowUser;

public class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, Unit>
{
	private readonly IFollowRepositoryAsync _followRepository;
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;
	private readonly INotificationService _notificationService;

	public FollowUserCommandHandler(
		IFollowRepositoryAsync followRepository,
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService,
		INotificationService notificationService)
	{
		_followRepository = followRepository;
		_context = context;
		_authenticatedUserService = authenticatedUserService;
		_notificationService = notificationService;
	}

	public async Task<Unit> Handle(FollowUserCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		if (currentUserId == request.UserId)
		{
			throw new SelfFollowException(currentUserId);
		}

		var currentUser = _context.Users.FirstOrDefault(user => user.Id == currentUserId);
		var targetUser = _context.Users.FirstOrDefault(user => user.Id == request.UserId);

		if (currentUser == null || targetUser == null)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		var alreadyFollowing = await _followRepository.IsFollowingAsync(currentUserId, request.UserId);
		if (alreadyFollowing)
		{
			return Unit.Value;
		}

		var hasBlockRelationship = _context.Blocks.Any(block =>
			(block.BlockerId == currentUserId && block.BlockedUserId == request.UserId) ||
			(block.BlockerId == request.UserId && block.BlockedUserId == currentUserId));

		if (hasBlockRelationship)
		{
			throw new ForbiddenException("You cannot follow this user while a block relationship exists.");
		}

		_context.Follows.Add(new Follow
		{
			FollowerId = currentUserId,
			FollowingId = request.UserId
		});

		currentUser.FollowingCount += 1;
		targetUser.FollowersCount += 1;
		await _context.SaveChangesAsync(cancellationToken);
		await _notificationService.CreateNotificationAsync(
			targetUser.Id,
			currentUserId,
			NotificationType.Follow,
			null,
			null);

		return Unit.Value;
	}
}