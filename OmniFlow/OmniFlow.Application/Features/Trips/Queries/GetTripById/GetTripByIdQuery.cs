using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQuery : IRequest<TripResponse>
{
    public Guid TripId { get; set; }

    public GetTripByIdQuery(Guid tripId)
    {
        TripId = tripId;
    }
}