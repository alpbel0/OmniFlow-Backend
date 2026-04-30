using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.DTOs.Providers;

public class GetProviderHotelsRequest
{
    public string City { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public BudgetTier? BudgetTier { get; set; }
    public int PersonCount { get; set; } = 1;
}