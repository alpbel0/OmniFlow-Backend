using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

/// <summary>
/// Response DTO for a saved trip, including saved date and trip details.
/// </summary>
public class SavedTripResponse
{
    public Guid TripId { get; set; }
    public DateTime SavedAt { get; set; }

    // Trip details
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public TripStatus Status { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; }
    public BudgetTier BudgetTier { get; set; }
    public TravelStyle TravelStyle { get; set; }
    public decimal? UserBudget { get; set; }
    public decimal? EstimatedCost { get; set; }
    public int ForkCount { get; set; }
    public int UpvoteCount { get; set; }
    public int ViewCount { get; set; }
    public decimal PopularityScore { get; set; }
    public List<string> Tags { get; set; } = new();

    // Owner info
    public Guid OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string? OwnerProfilePhotoUrl { get; set; }
}