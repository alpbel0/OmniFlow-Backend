using MediatR;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Trips.Commands.UpdateTrip;

public class UpdateTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PersonCount { get; set; }
    public BudgetTier BudgetTier { get; set; }
    public TravelStyle TravelStyle { get; set; }
    public decimal? UserBudget { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public List<string> Tags { get; set; } = new();
}