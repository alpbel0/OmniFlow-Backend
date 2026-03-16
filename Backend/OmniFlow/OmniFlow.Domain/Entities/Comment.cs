using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class Comment : AuditableBaseEntity
{
	public Guid PostId { get; set; }

	public Guid UserId { get; set; }

	public Guid? ParentCommentId { get; set; }

	public string Content { get; set; } = string.Empty;

	public List<string> Mentions { get; set; } = new();

	public int UpvoteCount { get; set; } = 0;

	public bool IsVisible { get; set; } = true;

	public Post? Post { get; set; }

	public User? User { get; set; }

	public Comment? ParentComment { get; set; }

	public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
