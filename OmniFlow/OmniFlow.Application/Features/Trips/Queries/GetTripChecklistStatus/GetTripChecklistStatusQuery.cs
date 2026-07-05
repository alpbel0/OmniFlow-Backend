using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripChecklistStatus;

public class GetTripChecklistStatusQuery : IRequest<TripChecklistStatusResponse>
{
	public Guid TripId { get; set; }
}
