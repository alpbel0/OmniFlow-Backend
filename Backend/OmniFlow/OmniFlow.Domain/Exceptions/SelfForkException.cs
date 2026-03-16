namespace OmniFlow.Domain.Exceptions;

public class SelfForkException : Exception
{
	public SelfForkException(Guid userId, Guid tripId)
		: base($"User '{userId}' cannot fork their own trip '{tripId}'.")
	{
	}
}
