using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Admin.Commands.AdminDeletePost;

public class AdminDeletePostCommandHandler : IRequestHandler<AdminDeletePostCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public AdminDeletePostCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(AdminDeletePostCommand request, CancellationToken cancellationToken)
	{
		await EnsureAdminAsync(cancellationToken);

		var post = await _context.Posts
			.FirstOrDefaultAsync(x => x.Id == request.PostId && x.DeletedAt == null, cancellationToken);
		if (post == null)
		{
			throw new EntityNotFoundException("Post", request.PostId);
		}

		post.DeletedAt = DateTime.UtcNow;
		post.UpdatedAt = DateTime.UtcNow;
		await _context.SaveChangesAsync(cancellationToken);
		return Unit.Value;
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
