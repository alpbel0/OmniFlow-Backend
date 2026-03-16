using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class CommunityTip : AuditableBaseEntity
{
	public Guid TripId { get; set; }

	public Guid UserId { get; set; }

	public Guid? PlaceId { get; set; }

	public string Content { get; set; } = string.Empty;

	public int UpvoteCount { get; set; } = 0;

	public bool IsVisible { get; set; } = true;

	public Trip? Trip { get; set; }

	public User? User { get; set; }

	public Place? Place { get; set; }
}
