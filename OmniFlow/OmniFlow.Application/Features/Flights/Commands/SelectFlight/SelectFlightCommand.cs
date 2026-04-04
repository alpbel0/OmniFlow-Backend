using MediatR;

namespace OmniFlow.Application.Features.Flights.Commands.SelectFlight;

public class SelectFlightCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public Guid FlightId { get; set; }
}