using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.ForkTrip;

public class ForkTripCommand : IRequest<Guid>
{
    public Guid TripId { get; set; }
}