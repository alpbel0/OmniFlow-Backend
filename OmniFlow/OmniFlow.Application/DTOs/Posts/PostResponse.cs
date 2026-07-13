using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Posts;

public class PostResponse
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
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
	public int UpvoteCount { get; set; }
	public int CommentCount { get; set; }
	public bool IsVisible { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }

	public string Username { get; set; } = string.Empty;
	public string? ProfilePhotoUrl { get; set; }
	public int KarmaScore { get; set; }

	public bool IsUpvoted { get; set; }
	public PostTripPreviewResponse? TripPreview { get; set; }
}

public class PostTripPreviewResponse
{
	public Guid Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? CoverPhotoUrl { get; set; }
	public string PrimaryLocation { get; set; } = string.Empty;
}
