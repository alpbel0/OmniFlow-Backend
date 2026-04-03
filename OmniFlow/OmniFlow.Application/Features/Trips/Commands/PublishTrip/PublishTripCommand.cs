using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.PublishTrip;

public class PublishTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}