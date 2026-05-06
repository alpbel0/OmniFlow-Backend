namespace OmniFlow.Application.DTOs.Admin;

public class AdminUserListItemResponse
{
	public Guid Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public string Role { get; set; } = string.Empty;
	public bool IsVerified { get; set; }
	public bool IsSuspended { get; set; }
	public int TripCount { get; set; }
	public int PostCount { get; set; }
	public DateTime CreatedAt { get; set; }
}
