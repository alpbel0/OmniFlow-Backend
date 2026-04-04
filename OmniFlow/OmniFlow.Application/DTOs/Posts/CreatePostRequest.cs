using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Posts;

public class CreatePostRequest
{
	public Guid? TripId { get; set; }
	public Guid? PlaceId { get; set; }
	public PostType PostType { get; set; }
	public string? Content { get; set; }
	public List<string> Photos { get; set; } = new();
	public List<string> Tags { get; set; } = new();
	public List<string> AiTags { get; set; } = new();
	public double? LocationLatitude { get; set; }
	public double? LocationLongitude { get; set; }
	public string? City { get; set; }
	public string? Country { get; set; }
}
