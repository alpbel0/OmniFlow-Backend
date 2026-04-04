using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public interface INotificationService
{
	Task CreateNotificationAsync(
		Guid userId,
		Guid? actorId,
		NotificationType type,
		Guid? targetId,
		NotificationTargetType? targetType);
}
