using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class User : AuditableBaseEntity
{
	public string Username { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string? Bio { get; set; }

	public string? ProfilePhotoUrl { get; set; }

	public string? Location { get; set; }

	public double? LocationLatitude { get; set; }

	public double? LocationLongitude { get; set; }

	public List<TravelStyle> TravelStyles { get; set; } = new();

	public int KarmaScore { get; set; } = 0;

	public int FollowersCount { get; set; } = 0;

	public int FollowingCount { get; set; } = 0;

	public Roles Role { get; set; } = Roles.Traveler;

	public bool IsVerified { get; set; } = false;

	public bool IsSuspended { get; set; } = false;

	public string? PreferredCurrencyCode { get; set; }

	public ICollection<Trip> Trips { get; set; } = new List<Trip>();

	public ICollection<Post> Posts { get; set; } = new List<Post>();

	public ICollection<PlaceVisitLog> VisitLogs { get; set; } = new List<PlaceVisitLog>();

	public ICollection<Follow> Followers { get; set; } = new List<Follow>();

	public ICollection<Follow> Following { get; set; } = new List<Follow>();

	public ICollection<Block> BlockedUsers { get; set; } = new List<Block>();

	public ICollection<Block> BlockedByUsers { get; set; } = new List<Block>();
}
