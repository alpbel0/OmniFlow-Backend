using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Trip : AuditableBaseEntity
{
	public Guid OwnerId { get; set; }

	public Guid? ForkedFromId { get; set; }

	public string Title { get; set; } = string.Empty;

	public string? Description { get; set; }

	public string? CoverPhotoUrl { get; set; }

	public TripStatus Status { get; set; } = TripStatus.Draft;

	public string City { get; set; } = string.Empty;

	public string Country { get; set; } = string.Empty;

	public DateOnly StartDate { get; set; }

	public DateOnly EndDate { get; set; }

	public int PersonCount { get; set; } = 1;

	public BudgetTier BudgetTier { get; set; }

	public TravelStyle TravelStyle { get; set; }

	public decimal? UserBudget { get; set; }

	public decimal? EstimatedCost { get; set; }

	public int ForkCount { get; set; } = 0;

	public int UpvoteCount { get; set; } = 0;

	public int ViewCount { get; set; } = 0;

	public decimal PopularityScore { get; set; } = 0;

	public List<string> Tags { get; set; } = new();

	public User? Owner { get; set; }

	public ICollection<Stop> Stops { get; set; } = new List<Stop>();

	public ICollection<Flight> Flights { get; set; } = new List<Flight>();

	public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}
