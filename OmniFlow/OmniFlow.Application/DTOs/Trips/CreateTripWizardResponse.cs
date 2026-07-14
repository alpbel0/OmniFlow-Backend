using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class CreateTripWizardResponse
{
    public Guid TripId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TripStatus Status { get; set; }
    public BudgetTier BudgetTier { get; set; }
    public BudgetTier? AdjustedBudgetTier { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ManualBudget { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";
    public List<string> BudgetMessages { get; set; } = new();
    public List<TripDestinationResponse> Destinations { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
