namespace OmniFlow.Application.DTOs.Users;

public class UserProfileResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Bio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
	public int KarmaScore { get; set; }
	public int FollowersCount { get; set; }
	public int FollowingCount { get; set; }
	public bool IsVerified { get; set; }
	public bool IsFollowing { get; set; }
	public int TripCount { get; set; }
	public int PostCount { get; set; }
}
