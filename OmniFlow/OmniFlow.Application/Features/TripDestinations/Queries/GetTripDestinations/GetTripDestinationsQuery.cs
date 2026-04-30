using MediatR;
using OmniFlow.Application.DTOs.Trips;

namespace OmniFlow.Application.Features.TripDestinations.Queries.GetTripDestinations;

public class GetTripDestinationsQuery : IRequest<IReadOnlyList<TripDestinationResponse>>
{
    public Guid TripId { get; set; }

    public GetTripDestinationsQuery(Guid tripId)
    {
        TripId = tripId;
    }
}
