using MediatR;

namespace OmniFlow.Application.Features.Stops.Commands.DeleteStop;

public class DeleteStopCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public Guid StopId { get; set; }
}