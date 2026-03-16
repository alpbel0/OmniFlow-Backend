using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class RefreshToken : BaseEntity
{
	public Guid UserId { get; set; }

	public string TokenHash { get; set; } = string.Empty;

	public DateTime ExpiresAt { get; set; }

	public DateTime? RevokedAt { get; set; }

	public string? DeviceFingerprint { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
