namespace OmniFlow.Application.DTOs.Admin;

public class AdminPostListItemResponse
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public string? Content { get; set; }
	public List<string> Photos { get; set; } = new();
	public List<string> Tags { get; set; } = new();
	public int UpvoteCount { get; set; }
	public int CommentCount { get; set; }
	public bool IsVisible { get; set; }
	public DateTime CreatedAt { get; set; }
}
