using MediatR;

namespace OmniFlow.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadCommand : IRequest<Unit>
{
	public Guid NotificationId { get; set; }
}
