using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Post : AuditableBaseEntity
{
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

	public int UpvoteCount { get; set; } = 0;

	public int CommentCount { get; set; } = 0;

	public bool IsVisible { get; set; } = true;

	public User? User { get; set; }

	public Trip? Trip { get; set; }

	public Place? Place { get; set; }

	public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
