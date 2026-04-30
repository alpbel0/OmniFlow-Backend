using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.GetBudgetSummary;

public class GetBudgetSummaryQuery : IRequest<BudgetSummaryResponse>
{
    public Guid TripId { get; set; }
}