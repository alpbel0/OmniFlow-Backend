namespace OmniFlow.Domain.Entities;

public class Follow
{
	public Guid FollowerId { get; set; }

	public Guid FollowingId { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public User? Follower { get; set; }

	public User? Following { get; set; }
}
