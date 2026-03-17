namespace OmniFlow.Domain.Exceptions;

public class DuplicateUpvoteException : Exception
{
	public DuplicateUpvoteException(string contentType, Guid contentId)
		: base($"User has already upvoted {contentType} with id '{contentId}'.")
	{
	}
}
