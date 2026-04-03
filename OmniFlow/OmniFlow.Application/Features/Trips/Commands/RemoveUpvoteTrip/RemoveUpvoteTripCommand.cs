using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.RemoveUpvoteTrip;

/// <summary>
/// Command to remove an upvote from a trip.
/// </summary>
public class RemoveUpvoteTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}