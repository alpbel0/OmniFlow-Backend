using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.UnpublishTrip;

public class UnpublishTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}
