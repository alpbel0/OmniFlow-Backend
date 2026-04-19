using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
	public Guid UserId { get; set; }

	public string TokenHash { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime ExpiresAt { get; set; }

	public DateTime? UsedAt { get; set; }
}