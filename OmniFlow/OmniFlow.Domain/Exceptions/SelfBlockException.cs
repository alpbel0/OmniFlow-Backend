namespace OmniFlow.Domain.Exceptions;

public class SelfBlockException : Exception
{
	public SelfBlockException(Guid userId)
		: base($"User '{userId}' cannot block themselves.")
	{
	}
}