using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.UnarchiveTrip;

public class UnarchiveTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}
