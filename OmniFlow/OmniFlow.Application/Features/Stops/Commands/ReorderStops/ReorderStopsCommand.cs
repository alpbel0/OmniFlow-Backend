using MediatR;

namespace OmniFlow.Application.Features.Stops.Commands.ReorderStops;

public class ReorderStopsCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
    public List<ReorderStopItem> Items { get; set; } = new();
}

public class ReorderStopItem
{
    public Guid StopId { get; set; }
    public int NewDayNumber { get; set; }
    public Guid? AfterStopId { get; set; }   // Insert after this stop
    public Guid? BeforeStopId { get; set; }  // Insert before this stop
}