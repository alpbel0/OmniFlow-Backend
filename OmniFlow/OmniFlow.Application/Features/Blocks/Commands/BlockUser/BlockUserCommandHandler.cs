using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Exceptions;

namespace OmniFlow.Application.Features.Blocks.Commands.BlockUser;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public BlockUserCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(BlockUserCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		if (currentUserId == request.UserId)
		{
			throw new SelfBlockException(currentUserId);
		}

		var currentUser = _context.Users.FirstOrDefault(user => user.Id == currentUserId);
		var targetUser = _context.Users.FirstOrDefault(user => user.Id == request.UserId);

		if (currentUser == null || targetUser == null)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		var alreadyBlocked = _context.Blocks.Any(block => block.BlockerId == currentUserId && block.BlockedUserId == request.UserId);
		if (alreadyBlocked)
		{
			return Unit.Value;
		}

		_context.Blocks.Add(new Block
		{
			BlockerId = currentUserId,
			BlockedUserId = request.UserId
		});

		var followToTarget = _context.Follows.FirstOrDefault(x => x.FollowerId == currentUserId && x.FollowingId == request.UserId);
		if (followToTarget != null)
		{
			_context.Follows.Remove(followToTarget);
			if (currentUser.FollowingCount > 0)
			{
				currentUser.FollowingCount -= 1;
			}

			if (targetUser.FollowersCount > 0)
			{
				targetUser.FollowersCount -= 1;
			}
		}

		var followFromTarget = _context.Follows.FirstOrDefault(x => x.FollowerId == request.UserId && x.FollowingId == currentUserId);
		if (followFromTarget != null)
		{
			_context.Follows.Remove(followFromTarget);
			if (targetUser.FollowingCount > 0)
			{
				targetUser.FollowingCount -= 1;
			}

			if (currentUser.FollowersCount > 0)
			{
				currentUser.FollowersCount -= 1;
			}
		}

		await _context.SaveChangesAsync(cancellationToken);
		return Unit.Value;
	}
}