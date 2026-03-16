namespace OmniFlow.Domain.Entities;

public class TripUpvote
{
	public Guid TripId { get; set; }

	public Guid UserId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
