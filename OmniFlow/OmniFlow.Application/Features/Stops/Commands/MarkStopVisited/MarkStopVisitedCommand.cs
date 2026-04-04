using MediatR;

namespace OmniFlow.Application.Features.Stops.Commands.MarkStopVisited;

public class MarkStopVisitedCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }
}