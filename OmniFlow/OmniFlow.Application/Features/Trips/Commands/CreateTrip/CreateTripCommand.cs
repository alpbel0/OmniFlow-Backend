using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTrip;

public class CreateTripCommand : IRequest<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Step 1: Origin city (departure point)
    public string Origin { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; } = 1;
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

    public string? CoverPhotoUrl { get; set; }
    public List<string> Tags { get; set; } = new();
}
