using MediatR;

namespace OmniFlow.Application.Features.TripDestinations.Commands.DeleteTripDestination;

public class DeleteTripDestinationCommand : IRequest<Unit>
{
    public Guid DestinationId { get; set; }
}
