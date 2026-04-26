using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Places;

public class PlaceResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlaceCategory Category { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsFree { get; set; }
    public string? PhotoUrl { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public List<string> GoogleTags { get; set; } = new();
    public List<BudgetTier> BudgetTiers { get; set; } = new();
    public List<TravelStyle> TravelStyles { get; set; } = new();
    public int? DurationMinutes { get; set; }
    public int? PriceLevel { get; set; }
    public int? ReviewCount { get; set; }
    public string? Wikipedia { get; set; }
    public string? Wikidata { get; set; }
    public string? Wheelchair { get; set; }
    public string? Heritage { get; set; }
    public string? Fee { get; set; }
    public string? Image { get; set; }
    public string? Cuisine { get; set; }
}