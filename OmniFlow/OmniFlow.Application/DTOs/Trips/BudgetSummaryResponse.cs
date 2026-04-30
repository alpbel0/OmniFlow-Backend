using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class BudgetSummaryResponse
{
    public decimal TotalFlightCost { get; set; }
    public decimal TotalHotelCost { get; set; }
    public decimal TotalActivityCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? ManualBudget { get; set; }
    public BudgetTier BudgetTier { get; set; }
    public BudgetTier? AdjustedBudgetTier { get; set; }
    public decimal SeasonMultiplier { get; set; }
    public List<string> Warnings { get; set; } = new();
}
