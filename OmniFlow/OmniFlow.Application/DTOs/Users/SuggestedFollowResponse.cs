namespace OmniFlow.Application.DTOs.Users;

public class SuggestedFollowResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public int TripCount { get; set; }
	public int KarmaScore { get; set; }
	public bool IsFollowing { get; set; }
	public string SuggestionReason { get; set; } = string.Empty;
}
