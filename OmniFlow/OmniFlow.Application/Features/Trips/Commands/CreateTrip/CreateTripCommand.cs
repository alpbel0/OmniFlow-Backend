using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.CreateTrip;

public class CreateTripCommand : IRequest<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; } = 1;
    public BudgetTier BudgetTier { get; set; }
    public TravelStyle TravelStyle { get; set; }
    public decimal? UserBudget { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public List<string> Tags { get; set; } = new();
}