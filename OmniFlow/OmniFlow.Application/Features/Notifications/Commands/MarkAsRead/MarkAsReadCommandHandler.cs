using MediatR;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, Unit>
{
	private readonly IApplicationDbContext _context;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public MarkAsReadCommandHandler(
		IApplicationDbContext context,
		IAuthenticatedUserService authenticatedUserService)
	{
		_context = context;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<Unit> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			throw new ForbiddenException("Invalid authenticated user.");
		}

		var notification = _context.Notifications.FirstOrDefault(x => x.Id == request.NotificationId);

		if (notification == null)
		{
			throw new EntityNotFoundException("Notification", request.NotificationId);
		}

		if (notification.UserId != currentUserId)
		{
			throw new ForbiddenException("You are not authorized to update this notification.");
		}

		if (!notification.IsRead)
		{
			notification.IsRead = true;
			notification.ReadAt = DateTime.UtcNow;
			await _context.SaveChangesAsync(cancellationToken);
		}

		return Unit.Value;
	}
}
