using MediatR;
using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, PagedResponse<NotificationResponse>>
{
	private readonly INotificationRepositoryAsync _notificationRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetNotificationsQueryHandler(
		INotificationRepositoryAsync notificationRepository,
		IAuthenticatedUserService authenticatedUserService)
	{
		_notificationRepository = notificationRepository;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<PagedResponse<NotificationResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			return new PagedResponse<NotificationResponse>(new List<NotificationResponse>(), 1, 10, 0);
		}

		var parameter = request.Parameter ?? new GetNotificationsParameter();
		var pageNumber = parameter.PageNumber > 0 ? parameter.PageNumber : 1;
		var pageSize = parameter.PageSize > 0 ? parameter.PageSize : 10;

		return await _notificationRepository.GetByUserAsync(
			currentUserId,
			parameter.IsRead,
			pageNumber,
			pageSize,
			cancellationToken);
	}
}
