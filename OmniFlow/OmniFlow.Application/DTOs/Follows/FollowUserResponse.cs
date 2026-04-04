namespace OmniFlow.Application.DTOs.Follows;

public class FollowUserResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public bool? IsFollowing { get; set; }
}