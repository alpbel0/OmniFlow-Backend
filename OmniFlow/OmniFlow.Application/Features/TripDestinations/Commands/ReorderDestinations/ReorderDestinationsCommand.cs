using MediatR;

namespace OmniFlow.Application.Features.TripDestinations.Commands.ReorderDestinations;

public class ReorderDestinationsCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public List<Guid> OrderedDestinationIds { get; set; } = new();
}
