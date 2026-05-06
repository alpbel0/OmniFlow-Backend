using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Admin.Commands.SetUserSuspended;

public class SetUserSuspendedCommandHandler : IRequestHandler<SetUserSuspendedCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public SetUserSuspendedCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(SetUserSuspendedCommand request, CancellationToken cancellationToken)
	{
		var currentUserId = await EnsureAdminAsync(cancellationToken);
		if (currentUserId == request.UserId)
		{
			throw new ForbiddenException("You cannot suspend your own admin account.");
		}

		var user = await _context.Users
			.FirstOrDefaultAsync(x => x.Id == request.UserId && x.DeletedAt == null, cancellationToken);
		if (user == null)
		{
			throw new EntityNotFoundException("User", request.UserId);
		}

		user.IsSuspended = request.IsSuspended;
		user.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync(cancellationToken);
		return Unit.Value;
	}

	private async Task<Guid> EnsureAdminAsync(CancellationToken cancellationToken)
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

		return currentUserId;
	}
}
