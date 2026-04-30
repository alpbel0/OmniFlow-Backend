using MediatR;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Providers.Queries.GetProviderHotels;

public class GetProviderHotelsQuery : IRequest<IReadOnlyList<ProviderHotelResponse>>
{
    public string City { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public BudgetTier? BudgetTier { get; set; }
    public int PersonCount { get; set; } = 1;
}