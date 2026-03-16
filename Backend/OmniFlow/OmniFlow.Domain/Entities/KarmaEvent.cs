using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class KarmaEvent : BaseEntity
{
	public Guid UserId { get; set; }

	public Guid? ActorId { get; set; }

	public KarmaEventType EventType { get; set; }

	public int Points { get; set; }

	public Guid? SourceId { get; set; }

	public KarmaSourceType? SourceType { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public User? User { get; set; }

	public User? Actor { get; set; }
}
