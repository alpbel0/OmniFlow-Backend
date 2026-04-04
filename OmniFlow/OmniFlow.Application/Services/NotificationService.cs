using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Services;

public class NotificationService : INotificationService
{
	private readonly IApplicationDbContext _context;

	public NotificationService(IApplicationDbContext context)
	{
		_context = context;
	}

	public async Task CreateNotificationAsync(
		Guid userId,
		Guid? actorId,
		NotificationType type,
		Guid? targetId,
		NotificationTargetType? targetType)
	{
		if (actorId.HasValue && actorId.Value == userId)
		{
			return;
		}

		if (type == NotificationType.Follow)
		{
			targetId = null;
			targetType = null;
		}

		await _context.Notifications.AddAsync(new Notification
		{
			UserId = userId,
			ActorId = actorId,
			NotificationType = type,
			TargetId = targetId,
			TargetType = targetType
		});

		await _context.SaveChangesAsync();
	}
}
