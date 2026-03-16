namespace OmniFlow.Application.Wrappers;

public class ErrorResponse
{
	public string Message { get; init; }
	public IReadOnlyList<string>? Errors { get; init; }

	public ErrorResponse(string message, IReadOnlyList<string>? errors = null)
	{
		Message = message;
		Errors = errors;
	}
}
