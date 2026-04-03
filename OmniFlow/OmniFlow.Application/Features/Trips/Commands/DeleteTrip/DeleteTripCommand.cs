using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.DeleteTrip;

public class DeleteTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}