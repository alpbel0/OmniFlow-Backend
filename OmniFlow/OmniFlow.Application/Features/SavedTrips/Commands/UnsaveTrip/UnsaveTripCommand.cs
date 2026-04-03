using MediatR;

namespace OmniFlow.Application.Features.SavedTrips.Commands.UnsaveTrip;

/// <summary>
/// Command to remove a trip from user's saved list.
/// </summary>
public class UnsaveTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}