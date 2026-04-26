using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Places.Commands.CreatePlace;

public class CreatePlaceCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PlaceCategory Category { get; set; }
    public string? PhotoUrl { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public List<string> GoogleTags { get; set; } = new();
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Timezone { get; set; }
    public string? GooglePlaceId { get; set; }
    public decimal EstimatedPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsFree { get; set; }
    public int? PriceLevel { get; set; }
    public int? ReviewCount { get; set; }
    public List<BudgetTier> BudgetTiers { get; set; } = new();
    public List<TravelStyle> TravelStyles { get; set; } = new();
    public int? DurationMinutes { get; set; }
    public decimal? Rating { get; set; }
    public string? OpeningHours { get; set; }
    public List<int> BestMonths { get; set; } = new();
    public string? Wikipedia { get; set; }
    public string? Wikidata { get; set; }
    public string? Wheelchair { get; set; }
    public string? Heritage { get; set; }
    public string? Fee { get; set; }
    public string? Image { get; set; }
    public string? Cuisine { get; set; }
}