using MediatR;
using OmniFlow.Application.DTOs.Stops;

namespace OmniFlow.Application.Features.Stops.Queries.GetStopsByTrip;

public class GetStopsByTripQuery : IRequest<List<StopResponse>>
{
    public Guid TripId { get; set; }

    public GetStopsByTripQuery(Guid tripId)
    {
        TripId = tripId;
    }
}