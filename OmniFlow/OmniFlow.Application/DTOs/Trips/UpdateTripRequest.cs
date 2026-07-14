using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class UpdateTripRequest
{
    public Guid TripId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Step 1: Origin city (departure point)
    public string Origin { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; }
    public BudgetTier BudgetTier { get; set; }

    // Step 4: Travel Companion
    public TravelCompanion TravelCompanion { get; set; }

    // Step 6: Vibe (max 3 styles)
    public List<TravelStyle> TravelStyles { get; set; } = new();

    // Step 7: Tempo
    public Tempo Tempo { get; set; }

    // Step 8: Transport Preference
    public TransportPreference TransportPreference { get; set; }

    // Step 5: Manual budget
    public decimal? ManualBudget { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";

    public string? CoverPhotoUrl { get; set; }
    public List<string> Tags { get; set; } = new();
}
