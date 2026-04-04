using OmniFlow.Application.DTOs.Places;

namespace OmniFlow.Application.DTOs.CommunityTips;

public class TipResponse
{
	public Guid Id { get; set; }
	public Guid TripId { get; set; }
	public Guid UserId { get; set; }
	public Guid? PlaceId { get; set; }
	public string Content { get; set; } = string.Empty;
	public int UpvoteCount { get; set; }
	public bool IsVisible { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public int KarmaScore { get; set; }

	public PlaceResponse? Place { get; set; }
	public bool IsUpvoted { get; set; }
}
