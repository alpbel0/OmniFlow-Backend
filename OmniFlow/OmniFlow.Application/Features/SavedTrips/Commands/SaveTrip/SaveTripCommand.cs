using MediatR;

namespace OmniFlow.Application.Features.SavedTrips.Commands.SaveTrip;

/// <summary>
/// Command to save a trip to user's saved list.
/// </summary>
public class SaveTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}