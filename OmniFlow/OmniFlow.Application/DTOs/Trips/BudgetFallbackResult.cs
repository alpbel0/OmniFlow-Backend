using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Trips;

public class BudgetFallbackResult
{
    public BudgetTier OriginalTier { get; set; }
    public BudgetTier AdjustedTier { get; set; }
    public bool IsAdjusted { get; set; }
    public List<string> Messages { get; set; } = new();
}
