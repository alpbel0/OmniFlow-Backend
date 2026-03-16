using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Notification : BaseEntity
{
	public Guid UserId { get; set; }

	public Guid? ActorId { get; set; }

	public NotificationType NotificationType { get; set; }

	public Guid? TargetId { get; set; }

	public NotificationTargetType? TargetType { get; set; }

	public bool IsRead { get; set; } = false;

	public DateTime? ReadAt { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public User? User { get; set; }

	public User? Actor { get; set; }
}
