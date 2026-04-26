using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Queries.ExploreTrips;

public class ExploreTripsParameter
{
    // Filters
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SearchTerm { get; set; }
    public BudgetTier? BudgetTier { get; set; }
    public List<TravelStyle>? TravelStyles { get; set; }
    public List<string>? Tags { get; set; }

    // Sorting & Pagination
    public string SortBy { get; set; } = "popularity_score";
    public int PageSize { get; set; } = 10;
    public string? Cursor { get; set; }
}
