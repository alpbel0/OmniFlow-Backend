namespace OmniFlow.Application.DTOs.Posts;

public class UpdatePostRequest
{
	public string? Content { get; set; }
	public List<string>? Tags { get; set; }
}
