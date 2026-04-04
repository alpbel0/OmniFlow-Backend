using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface INotificationRepositoryAsync
{
	Task<PagedResponse<NotificationResponse>> GetByUserAsync(
		Guid userId,
		bool? isRead,
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default);

	Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
