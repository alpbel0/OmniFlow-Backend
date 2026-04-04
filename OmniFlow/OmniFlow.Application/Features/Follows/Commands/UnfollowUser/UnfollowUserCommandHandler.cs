using MediatR;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Follows.Commands.UnfollowUser;

public class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public UnfollowUserCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
		var follow = _context.Follows.FirstOrDefault(x => x.FollowerId == currentUserId && x.FollowingId == request.UserId);

		if (follow == null)
		{
			return Unit.Value;
		}

		var follower = follow.Follower ?? _context.Users.FirstOrDefault(user => user.Id == currentUserId);
		var following = follow.Following ?? _context.Users.FirstOrDefault(user => user.Id == request.UserId);

		if (follower != null && follower.FollowingCount > 0)
		{
			follower.FollowingCount -= 1;
		}

		if (following != null && following.FollowersCount > 0)
		{
			following.FollowersCount -= 1;
		}

		_context.Follows.Remove(follow);
		await _context.SaveChangesAsync(cancellationToken);

		return Unit.Value;
	}
}