using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class ScoredPlaceResponse
{
    // Place properties for frontend card rendering
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlaceCategory Category { get; set; }
    public string? PhotoUrl { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal EstimatedPrice { get; set; }
    public decimal? Rating { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsFree { get; set; }
    public List<BudgetTier> BudgetTiers { get; set; } = new();
    public List<string> GoogleTags { get; set; } = new();

    // Scoring
    public int FinalScore { get; set; }
    public int GroupScore { get; set; }
    public int StyleScoreAvg { get; set; }
    public int GoogleMatchBonus { get; set; }
}
