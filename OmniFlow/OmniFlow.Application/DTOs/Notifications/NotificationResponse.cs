using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Notifications;

public class NotificationResponse
{
	public Guid Id { get; set; }
	public NotificationType Type { get; set; }
	public Guid? TargetId { get; set; }
	public NotificationTargetType? TargetType { get; set; }
	public bool IsRead { get; set; }
	public DateTime? ReadAt { get; set; }
	public DateTime CreatedAt { get; set; }

	public string? ActorUsername { get; set; }
	public string? ActorProfilePhotoUrl { get; set; }
}
