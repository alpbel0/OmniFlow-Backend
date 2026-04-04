using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Comments;

public class CommentResponse
{
	public Guid Id { get; set; }
	public Guid PostId { get; set; }
	public Guid UserId { get; set; }
	public Guid? ParentCommentId { get; set; }
	public string Content { get; set; } = string.Empty;
	public List<string> Mentions { get; set; } = new();
	public int UpvoteCount { get; set; }
	public bool IsVisible { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public int KarmaScore { get; set; }

	public bool IsUpvoted { get; set; }
	public List<CommentResponse> Replies { get; set; } = new();
}
