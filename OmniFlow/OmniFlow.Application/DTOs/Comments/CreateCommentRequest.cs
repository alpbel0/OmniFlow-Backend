namespace OmniFlow.Application.DTOs.Comments;

public class CreateCommentRequest
{
	public Guid? ParentCommentId { get; set; }
	public string Content { get; set; } = string.Empty;
	public List<string> Mentions { get; set; } = new();
}
