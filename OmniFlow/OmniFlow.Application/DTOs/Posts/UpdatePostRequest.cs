using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Posts;

public class UpdatePostRequest
{
	public string? Content { get; set; }
	public List<string>? Tags { get; set; }
	public Guid? TripId { get; set; }
	public Guid? PlaceId { get; set; }
	public PostType? PostType { get; set; }
	public List<string>? Photos { get; set; }
	public List<string>? AiTags { get; set; }
}
