namespace OmniFlow.Domain.Common;

public abstract class AuditableBaseEntity : BaseEntity
{
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public DateTime? DeletedAt { get; set; }
}
