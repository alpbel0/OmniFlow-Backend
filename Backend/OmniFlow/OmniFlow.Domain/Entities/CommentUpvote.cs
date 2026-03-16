namespace OmniFlow.Domain.Entities;

public class CommentUpvote
{
	public Guid CommentId { get; set; }

	public Guid UserId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
