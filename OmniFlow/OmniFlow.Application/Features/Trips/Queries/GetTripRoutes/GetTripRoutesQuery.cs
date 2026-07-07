using MediatR;
using OmniFlow.Application.DTOs.Routes;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripRoutes;

public class GetTripRoutesQuery : IRequest<TripRoutesResponse>
{
    public Guid TripId { get; set; }
}
