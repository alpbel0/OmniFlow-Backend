namespace OmniFlow.Domain.Entities;

public class TipUpvote
{
	public Guid TipId { get; set; }

	public Guid UserId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
