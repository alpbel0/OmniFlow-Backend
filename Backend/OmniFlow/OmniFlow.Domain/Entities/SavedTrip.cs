namespace OmniFlow.Domain.Entities;

public class SavedTrip
{
	public Guid UserId { get; set; }

	public Guid TripId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
