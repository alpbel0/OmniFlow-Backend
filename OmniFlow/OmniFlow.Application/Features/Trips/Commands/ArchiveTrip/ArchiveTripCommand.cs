using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.ArchiveTrip;

public class ArchiveTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}