namespace OmniFlow.Domain.Entities;

public class Block
{
	public Guid BlockerId { get; set; }

	public Guid BlockedUserId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public User? Blocker { get; set; }

	public User? BlockedUser { get; set; }
}