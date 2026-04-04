using MediatR;

namespace OmniFlow.Application.Features.Flights.Queries.GetFlightsByTrip;

public class GetFlightsByTripQuery : IRequest<FlightsByTripViewModel>
{
    public Guid TripId { get; }

    public GetFlightsByTripQuery(Guid tripId)
    {
        TripId = tripId;
    }
}