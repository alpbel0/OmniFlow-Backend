namespace OmniFlow.Application.Exceptions;

public class ApiException : Exception
{
	public int StatusCode { get; }
	public string? Code { get; }

	public ApiException(string message, int statusCode = 400, string? code = null)
		: base(message)
	{
		StatusCode = statusCode;
		Code = code;
	}
}
