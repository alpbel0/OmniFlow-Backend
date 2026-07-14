using OmniFlow.Application.Features.Trips.Queries.SearchNearbyPlaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public sealed class NearbyPlaceResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public PlaceCategory Category { get; init; }
    public string? PhotoUrl { get; init; }
    public List<string> PhotoUrls { get; init; } = [];
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public decimal EstimatedPrice { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public int? PriceLevel { get; init; }
    public decimal? Rating { get; init; }
    public int? ReviewCount { get; init; }
    public int? DurationMinutes { get; init; }
    public bool IsFree { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? Cuisine { get; init; }
    public List<BudgetTier> BudgetTiers { get; init; } = [];
    public List<string> GoogleTags { get; init; } = [];
    public int DistanceMeters { get; init; }
    public bool IsPreviouslyVisited { get; init; }
    public int PreviousVisitCount { get; init; }
    public int PersonalizationScore { get; init; }
    public NearbyPersonalizationTier PersonalizationTier { get; init; }
}
