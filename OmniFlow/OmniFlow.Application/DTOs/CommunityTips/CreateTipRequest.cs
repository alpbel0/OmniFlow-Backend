namespace OmniFlow.Application.DTOs.CommunityTips;

public class CreateTipRequest
{
	public Guid TripId { get; set; }

	public Guid? PlaceId { get; set; }

	public string Content { get; set; } = string.Empty;
}
