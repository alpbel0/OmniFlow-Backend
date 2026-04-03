using MediatR;

namespace OmniFlow.Application.Features.Trips.Commands.UpvoteTrip;

/// <summary>
/// Command to upvote a published trip.
/// </summary>
public class UpvoteTripCommand : IRequest<Unit>
{
    public Guid TripId { get; set; }
}