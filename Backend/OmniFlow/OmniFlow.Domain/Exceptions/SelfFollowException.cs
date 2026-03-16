namespace OmniFlow.Domain.Exceptions;

public class SelfFollowException : Exception
{
	public SelfFollowException(Guid userId)
		: base($"User '{userId}' cannot follow themselves.")
	{
	}
}
