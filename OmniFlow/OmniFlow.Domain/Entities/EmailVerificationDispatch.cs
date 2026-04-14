using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class EmailVerificationDispatch : AuditableBaseEntity
{
	public Guid? UserId { get; set; }
	public string Email { get; set; } = string.Empty;
	public string Purpose { get; set; } = "email-verification";
	public DateTime SentAt { get; set; } = DateTime.UtcNow;
}