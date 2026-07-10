using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class TripResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public TripStatus Status { get; set; }

    // Step 1: Origin
    public string Origin { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public double? OriginLatitude { get; set; }
    public double? OriginLongitude { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; }
    public BudgetTier BudgetTier { get; set; }

    // Wizard fields
    public TravelCompanion TravelCompanion { get; set; }
    public List<TravelStyle> TravelStyles { get; set; } = new();
    public Tempo Tempo { get; set; }
    public TransportPreference TransportPreference { get; set; }
    public decimal? ManualBudget { get; set; }
    public BudgetTier? AdjustedBudgetTier { get; set; }

    public decimal? EstimatedCost { get; set; }
    public int CompletionPercentage { get; set; }
    public int ForkCount { get; set; }
    public int UpvoteCount { get; set; }
    public int ViewCount { get; set; }
    public decimal PopularityScore { get; set; }
    public Guid? ForkedFromId { get; set; }
    public List<string> Tags { get; set; } = new();

    // Owner info
    public Guid OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string? OwnerProfilePhotoUrl { get; set; }

    // User-specific flags (null for unauthenticated users)
    public bool? IsUpvoted { get; set; }
    public bool? IsSaved { get; set; }

    // Multi-destination legs
    public List<TripDestinationResponse> Destinations { get; set; } = new();

    // Timeline summary (daily entry counts)
    public TimelineSummary? TimelineSummary { get; set; }
}
