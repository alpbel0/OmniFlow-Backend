namespace OmniFlow.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user attempts to upvote their own trip.
/// </summary>
public class SelfUpvoteException : Exception
{
    public SelfUpvoteException(Guid userId)
        : base($"User '{userId}' cannot upvote their own trip.")
    {
    }
}