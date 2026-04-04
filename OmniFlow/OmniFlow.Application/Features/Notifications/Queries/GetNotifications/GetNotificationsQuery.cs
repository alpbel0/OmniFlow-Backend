using MediatR;
using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQuery : IRequest<PagedResponse<NotificationResponse>>
{
	public GetNotificationsParameter Parameter { get; set; } = new();
}
