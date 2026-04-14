namespace OmniFlow.Application.DTOs.Blocks;

public class BlockedUserResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public DateTime BlockedAt { get; set; }
	public bool? IsFollowing { get; set; }
}