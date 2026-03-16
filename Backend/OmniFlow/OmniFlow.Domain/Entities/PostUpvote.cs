namespace OmniFlow.Domain.Entities;

public class PostUpvote
{
	public Guid PostId { get; set; }

	public Guid UserId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
