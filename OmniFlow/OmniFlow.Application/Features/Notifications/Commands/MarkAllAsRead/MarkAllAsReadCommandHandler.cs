using MediatR;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllAsReadCommandHandler : IRequestHandler<MarkAllAsReadCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public MarkAllAsReadCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(MarkAllAsReadCommand request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			return Unit.Value;
		}

		var unreadNotifications = _context.Notifications
			.Where(notification => notification.UserId == currentUserId && !notification.IsRead)
			.ToList();

		if (unreadNotifications.Count == 0)
		{
			return Unit.Value;
		}

		var readAt = DateTime.UtcNow;

		foreach (var notification in unreadNotifications)
		{
			notification.IsRead = true;
			notification.ReadAt = readAt;
		}

		await _context.SaveChangesAsync(cancellationToken);
		return Unit.Value;
	}
}
