using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
	private readonly INotificationRepositoryAsync _notificationRepository;
	private readonly IAuthenticatedUserService _authenticatedUserService;

	public GetUnreadCountQueryHandler(
		INotificationRepositoryAsync notificationRepository,
		IAuthenticatedUserService authenticatedUserService)
	{
		_notificationRepository = notificationRepository;
		_authenticatedUserService = authenticatedUserService;
	}

	public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_authenticatedUserService.UserId, out var currentUserId))
		{
			return 0;
		}

		return await _notificationRepository.GetUnreadCountAsync(currentUserId, cancellationToken);
	}
}
