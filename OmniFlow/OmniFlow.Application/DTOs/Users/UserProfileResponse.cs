using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Users;

public class UserProfileResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? Bio { get; set; }
	public string? ProfilePhotoUrl { get; set; }
	public string? Location { get; set; }
	public double? LocationLatitude { get; set; }
	public double? LocationLongitude { get; set; }
	public List<TravelStyle> TravelStyles { get; set; } = new();
	public int KarmaScore { get; set; }
	public int FollowersCount { get; set; }
	public int FollowingCount { get; set; }
	public bool IsVerified { get; set; }
	public bool IsFollowing { get; set; }
	public int TripCount { get; set; }
	public int PostCount { get; set; }
	public bool IsBlocked { get; set; }
	public bool IsBlockedByMe { get; set; }
}
